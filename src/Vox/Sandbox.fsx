#r "nuget: Plotly.NET"

#load "Library.fs"

open System
open Vox

// --- Sandbox Experiments ---

let sampleRate = 44100.0f
let provider = StackBufferProvider(1024) :> IScratchProvider
let ctx = {
    Scratch = provider
    SampleRate = sampleRate
}

// Let's create a simple signal: (Parameter 0.5 + Parameter 0.2) * 0.5
let signal = Scale(Add(Parameter(0.5f), Parameter(0.2f)), 0.5f) :> ISignal

let buffer = Array.zeroCreate<float32> 10

do
    let span = Span<float32>(buffer)
    signal.Fill(ctx, span)

printfn "Signal values: %A" buffer
// Expected: [0.35; 0.35; ...] since (0.5 + 0.2) * 0.5 = 0.35
