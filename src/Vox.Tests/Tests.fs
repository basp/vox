module Tests

open System
open Xunit
open Vox

[<Fact>]
let ``StackBufferProvider can rent a buffer`` () =
    let provider = StackBufferProvider(100) :> IScratchProvider
    use buffer = provider.GetBuffer(10)
    Assert.Equal(10, buffer.Segment.Count)

[<Fact>]
let ``StackBufferProvider resets offset on disposal (LIFO)`` () =
    let provider = StackBufferProvider(100) :> IScratchProvider
    
    // First allocation
    let buf1 = provider.GetBuffer(10)
    Assert.Equal(0, buf1.Segment.Offset)
    
    // Second allocation
    let buf2 = provider.GetBuffer(20)
    Assert.Equal(10, buf2.Segment.Offset)
    
    // Dispose second (LIFO - this should move currentOffset back to 10)
    buf2.Dispose()
    
    // Third allocation (should reuse buf2's space)
    let buf3 = provider.GetBuffer(5)
    Assert.Equal(10, buf3.Segment.Offset)
    
    buf3.Dispose()
    buf1.Dispose()

[<Fact>]
let ``StackBufferProvider throws when exhausted`` () =
    let provider = StackBufferProvider(10) :> IScratchProvider
    
    // Fill it up
    use _buf1 = provider.GetBuffer(10)
    
    // Next one should fail
    Assert.Throws<Exception>(fun () -> provider.GetBuffer(1) |> ignore)
    |> ignore

[<Fact>]
let ``StackBufferProvider handles multiple allocations up to capacity`` () =
    let provider = StackBufferProvider(15) :> IScratchProvider
    use buf1 = provider.GetBuffer(5)
    use buf2 = provider.GetBuffer(10)
    Assert.Equal(0, buf1.Segment.Offset)
    Assert.Equal(5, buf2.Segment.Offset)

let private makeCtx () : RenderContext =
    { Scratch = ArrayPoolBufferProvider() :> IScratchProvider; SampleRate = 44100.0f }

let private fill (signal: ISignal) (ctx: RenderContext) (n: int) =
    let buffer = Array.zeroCreate<float32> n
    signal.Fill(ctx, Span<float32>(buffer))
    buffer

[<Fact>]
let ``Parameter fills buffer with initial value`` () =
    let ctx = makeCtx()
    let buffer = fill (Variable(0.5f)) ctx 4
    Assert.All(buffer, fun v -> Assert.Equal(0.5f, v))

[<Fact>]
let ``Parameter value can be updated`` () =
    let ctx = makeCtx()
    let param = Variable(0.5f)
    param.Value <- 1.0f
    let buffer = fill param ctx 4
    Assert.All(buffer, fun v -> Assert.Equal(1.0f, v))

[<Fact>]
let ``Sine produces correct values over one cycle`` () =
    // sampleRate=4, frequency=1: increment=0.25 per sample
    // phases: 0, 0.25, 0.5, 0.75 -> sin: 0, 1, ~0, -1
    let ctx = { Scratch = ArrayPoolBufferProvider() :> IScratchProvider; SampleRate = 4.0f }
    let buffer = fill (Sine(Variable(1.0f))) ctx 4
    Assert.Equal(0.0f,  buffer[0], 5)
    Assert.Equal(1.0f,  buffer[1], 5)
    Assert.Equal(0.0f,  buffer[2], 5)
    Assert.Equal(-1.0f, buffer[3], 5)

[<Fact>]
let ``Saw produces correct values over one cycle`` () =
    // phases: 0, 0.25, 0.5, 0.75 -> 2x - 1: -1.0, -0.5, 0.0, 0.5
    let ctx = { Scratch = ArrayPoolBufferProvider() :> IScratchProvider; SampleRate = 4.0f }
    let buffer = fill (Saw(Variable(1.0f))) ctx 5
    Assert.Equal(-1.0f, buffer[0], 5)
    Assert.Equal(-0.5f, buffer[1], 5)
    Assert.Equal(0.0f,  buffer[2], 5)
    Assert.Equal(0.5f,  buffer[3], 5)
    Assert.Equal(-1.0f,  buffer[4], 5)

[<Fact>]
let ``Square produces correct values over one cycle`` () =
    // phases: 0, 0.25, 0.5, 0.75 -> square: 1.0, 1.0, -1.0, -1.0
    let ctx = { Scratch = ArrayPoolBufferProvider() :> IScratchProvider; SampleRate = 4.0f }
    let buffer = fill (Square(Variable(1.0f))) ctx 4
    Assert.Equal(1.0f,  buffer[0], 5)
    Assert.Equal(1.0f,  buffer[1], 5)
    Assert.Equal(-1.0f, buffer[2], 5)
    Assert.Equal(-1.0f, buffer[3], 5)

[<Fact>]
let ``Triangle produces correct values over one cycle`` () =
    // phases: 0, 0.25, 0.5, 0.75
    // 2x - 1: -1, -0.5, 0, 0.5
    // abs(2x-1): 1, 0.5, 0, 0.5
    // 2*abs(2x-1)-1: 1.0, 0.0, -1.0, 0.0
    let ctx = { Scratch = ArrayPoolBufferProvider() :> IScratchProvider; SampleRate = 4.0f }
    let buffer = fill (Triangle(Variable(1.0f))) ctx 4
    Assert.Equal(1.0f,  buffer[0], 5)
    Assert.Equal(0.0f,  buffer[1], 5)
    Assert.Equal(-1.0f, buffer[2], 5)
    Assert.Equal(0.0f,  buffer[3], 5)

[<Fact>]
let ``Sine Reset restarts phase from zero`` () =
    let ctx = makeCtx()
    let osc = Sine(Variable(440.0f))
    // Advance phase partway through a block
    (osc :> ISignal).Fill(ctx, Span<float32>(Array.zeroCreate 512))
    (osc :> ISignal).Reset()
    // After reset the first sample must be sin(0) = 0
    let buffer = fill osc ctx 1
    Assert.Equal(0.0, float buffer[0], 5)
