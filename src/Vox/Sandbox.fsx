#r "nuget: Plotly.NET"

open System
open System.Buffers

type ISignal =
    abstract member Fill: buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit
    
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
            let pool = ArrayPool<float32>.Shared
            let tmp = pool.Rent(buffer.Length)
            try
                a.Fill(tmp)
                b.Fill(buffer)
                for i in 0..buffer.Length - 1 do
                    buffer[i] <- tmp[i] * buffer[i]
            finally
                pool.Return(tmp)
        member _.Reset() =
            a.Reset()
            b.Reset()
            
type Add(a: ISignal, b: ISignal) =
    interface ISignal with
        member _.Fill(buffer: Span<float32>) =
            let pool = ArrayPool<float32>.Shared
            let tmp = pool.Rent(buffer.Length)
            try
                a.Fill(tmp)
                b.Fill(buffer)
                for i in 0..buffer.Length - 1 do
                    buffer[i] <- tmp[i] + buffer[i]
            finally
                pool.Return(tmp)
        member _.Reset() =
            a.Reset()
            b.Reset()
            
let buffer = Array.zeroCreate<float32> 8
let signal = Clamp(Scale(Constant(1.0f), 2.0f), 0.0f, 1.5f) :> ISignal
signal.Fill(buffer)
