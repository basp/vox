namespace Vox

open System
open System.Buffers

type IScratchBuffer =
    inherit IDisposable
    abstract member Segment: ArraySegment<float32>
    
type IScratchProvider =
    abstract member GetBuffer: length: int -> IScratchBuffer

type RenderContext = {
    Scratch: IScratchProvider
    SampleRate: float32
}

type ArrayPoolBuffer(array: float32[], length: int) =
    interface IScratchBuffer with
        member _.Segment =
            ArraySegment(array, 0, length)
            
        member _.Dispose() =
            ArrayPool<float32>.Shared.Return(array)

type ArrayPoolBufferProvider() =
    interface IScratchProvider with
        member _.GetBuffer(length: int) =
            let rented = ArrayPool<float32>.Shared.Rent(length)
            new ArrayPoolBuffer(rented, length)

type StackBuffer(array: float32[], offset: int, length: int, onDispose: unit -> unit) =
    interface IScratchBuffer with
        member _.Segment =
            ArraySegment(array, offset, length)
        member _.Dispose() =
            onDispose()

type StackBufferProvider(maxCapacity: int) =
    let pool: float32[] = Array.zeroCreate maxCapacity
    let mutable currentOffset = 0
    
    interface IScratchProvider with
        member _.GetBuffer(length) =
            if currentOffset + length > maxCapacity
            then failwith "Scratch pool exhausted!"
            let start = currentOffset
            currentOffset <- currentOffset + length
            new StackBuffer(pool, start, length, fun () -> currentOffset <- start)

type ISignal =
    abstract member Fill: ctx: RenderContext * buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit

type IOscillator =
    abstract member Frequency: float32 with get, set

module private Interpolate =
    let inline lerp (a: float32) (b: float32) (t: float32) =
        a + (b - a) * t

module private BinaryOp =
    let inline rent
        (ctx: RenderContext)
        (a: ISignal)
        (b: ISignal)
        ([<InlineIfLambda>] op: float32 -> float32 -> float32)
        (buffer: Span<float32>) =
            use scratch = ctx.Scratch.GetBuffer(buffer.Length)
            let tmp = scratch.Segment.AsSpan()
            a.Fill(ctx, tmp)
            b.Fill(ctx, buffer)
            for i in 0..buffer.Length - 1 do
                buffer[i] <- op tmp[i] buffer[i]
        
    let inline reset (a: ISignal) (b: ISignal) =
        a.Reset()
        b.Reset()
    
module private TernaryOp =
    let inline rent
        (ctx: RenderContext)
        (a: ISignal)
        (b: ISignal)
        (c: ISignal)
        ([<InlineIfLambda>] op: float32 -> float32 -> float32 -> float32)
        (buffer: Span<float32>) =
            use bufA = ctx.Scratch.GetBuffer(buffer.Length)
            use bufC = ctx.Scratch.GetBuffer(buffer.Length)
            let tmpA = bufA.Segment.AsSpan()
            let tmpC = bufC.Segment.AsSpan()
            a.Fill(ctx, tmpA)
            b.Fill(ctx, buffer)
            c.Fill(ctx, tmpC)
            for i in 0..buffer.Length - 1 do
                buffer[i] <- op tmpA[i] buffer[i] tmpC[i]
                
    let inline reset (a: ISignal) (b: ISignal) (c: ISignal) =
        a.Reset()
        b.Reset()
        c.Reset()
    
type Constant(value: float32) =
    interface ISignal with
        member _.Fill(_, buffer) =
            buffer.Fill(value)
        member _.Reset() =
            ()
        
type Scale(signal: ISignal, factor: float32) =
    interface ISignal with
        member _.Fill(ctx, buffer) =
            signal.Fill(ctx, buffer)
            for i in 0..buffer.Length - 1 do
                buffer[i] <- buffer[i] * factor
        member _.Reset() =
            signal.Reset()

type Clamp(signal: ISignal, min: float32, max: float32) =
    do
        if min > max then
            ArgumentException("min must be less than or equal to max")
            |> raise
            
    let clamp (x: float32) =
        if x < min then min
        elif x > max then max
        else x
    
    interface ISignal with
        member _.Fill(ctx, buffer) =
            signal.Fill(ctx, buffer)
            for i in 0..buffer.Length - 1 do
                buffer[i] <- clamp buffer[i]

        member _.Reset() =
            signal.Reset()
        
type Multiply(a: ISignal, b: ISignal) =
    interface ISignal with
        member _.Fill(ctx, buffer) =
            BinaryOp.rent ctx a b (*) buffer

        member _.Reset() =
            BinaryOp.reset a b
            
type Add(a: ISignal, b: ISignal) =
    interface ISignal with
        member _.Fill(ctx, buffer) =
            BinaryOp.rent ctx a b (+) buffer

        member _.Reset() =
            BinaryOp.reset a b

type Mix(a: ISignal, b: ISignal, control: ISignal) =
    let mix x y c =
        let w = (c + 1.0f) * 0.5f
        Interpolate.lerp x y w
        
    interface ISignal with        
        member _.Fill(ctx, buffer) =
            TernaryOp.rent ctx a b control mix buffer

        member _.Reset() =
            TernaryOp.reset a b control
