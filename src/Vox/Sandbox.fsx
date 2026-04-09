#r "nuget: Plotly.NET"

open System
open System.Buffers

type ISignal =
    abstract member Fill: buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit

module private BinaryOp =
    let inline withRented
        (a: ISignal)
        (b: ISignal)
        ([<InlineIfLambda>] op: float32 -> float32 -> float32)
        (buffer: Span<float32>) =
            let pool = ArrayPool<float32>.Shared
            let rented = pool.Rent(buffer.Length)
            try
                let tmp = rented.AsSpan(0, buffer.Length)
                a.Fill(tmp)
                b.Fill(buffer)
                for i in 0..buffer.Length - 1 do
                    buffer[i] <- op tmp[i] buffer[i]
            finally
                pool.Return(rented)    
        
    let reset (a: ISignal) (b: ISignal) =
        a.Reset()
        b.Reset()
    
type Constant(value: float32) =
    interface ISignal with
        member _.Fill(buffer: Span<float32>) =
            buffer.Fill(value)
        member _.Reset() =
            ()
        
type Scale(signal: ISignal, factor: float32) =
    interface ISignal with
        member _.Fill(buffer: Span<float32>) =
            signal.Fill(buffer)
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
        member _.Fill(buffer: Span<float32>) =
            signal.Fill(buffer)
            for i in 0..buffer.Length - 1 do
                buffer[i] <- clamp buffer[i]
        member _.Reset() =
            signal.Reset()
        
type Multiply(a: ISignal, b: ISignal) =
    interface ISignal with
        member _.Fill(buffer: Span<float32>) =
            BinaryOp.withRented a b (*) buffer
        member _.Reset() =
            BinaryOp.reset a b
            
type Add(a: ISignal, b: ISignal) =
    interface ISignal with
        member _.Fill(buffer: Span<float32>) =
            BinaryOp.withRented a b (+) buffer
        member _.Reset() =
            BinaryOp.reset a b
            
