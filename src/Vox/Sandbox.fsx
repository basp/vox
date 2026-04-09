#r "nuget: Plotly.NET"

open System

type RenderContext(initialBlockSize: int, scratchCount: int) =
    let mutable blockSize = max 1 initialBlockSize
    
    let scratches: float32[][] =
        Array.init scratchCount (fun _ -> Array.zeroCreate blockSize)    
    
    member this.Scratch(slot: int, length: int) =
        this.EnsureCapacity(length)
        scratches[slot].AsSpan(0, length)
    
    member _.EnsureCapacity(required: int) =
        if required > blockSize then
            blockSize <- required
            for i in 0 .. scratches.Length - 1 do
                if scratches[i].Length < required then
                    scratches[i] <- Array.zeroCreate required

type ISignal =
    abstract member Fill: ctx: RenderContext * buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit

type IOscillator =
    abstract member Frequency: float32 with get, set

module private Interpolate =
    let inline lerp (a: float32) (b: float32) (t: float32) =
        a + (b - a) * t

module private BinaryOp =
    let inline withRented
        (ctx: RenderContext)
        (a: ISignal)
        (b: ISignal)
        ([<InlineIfLambda>] op: float32 -> float32 -> float32)
        (buffer: Span<float32>) =
            let tmp = ctx.Scratch(0, buffer.Length)
            a.Fill(ctx, tmp)
            b.Fill(ctx, buffer)
            for i in 0..buffer.Length - 1 do
                buffer[i] <- op tmp[i] buffer[i]
        
    let inline reset (a: ISignal) (b: ISignal) =
        a.Reset()
        b.Reset()
    
module private TernaryOp =
    let inline withRented
        (ctx: RenderContext)
        (a: ISignal)
        (b: ISignal)
        (c: ISignal)
        ([<InlineIfLambda>] op: float32 -> float32 -> float32 -> float32)
        (buffer: Span<float32>) =
            let tmpA = ctx.Scratch(0, buffer.Length)
            let tmpC = ctx.Scratch(1, buffer.Length)
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
            BinaryOp.withRented ctx a b (*) buffer

        member _.Reset() =
            BinaryOp.reset a b
            
type Add(a: ISignal, b: ISignal) =
    interface ISignal with
        member _.Fill(ctx, buffer) =
            BinaryOp.withRented ctx a b (+) buffer

        member _.Reset() =
            BinaryOp.reset a b

type Mix(a: ISignal, b: ISignal, control: ISignal) =
    let mix x y c =
        let w = (c + 1.0f) * 0.5f
        Interpolate.lerp x y w
        
    interface ISignal with        
        member _.Fill(ctx, buffer) =
            TernaryOp.withRented ctx a b control mix buffer

        member _.Reset() =
            TernaryOp.reset a b control            
