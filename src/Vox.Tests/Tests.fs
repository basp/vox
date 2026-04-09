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
    Assert.Throws<Exception>(fun () -> 
        provider.GetBuffer(1) |> ignore
    ) |> ignore

[<Fact>]
let ``StackBufferProvider handles multiple allocations up to capacity`` () =
    let provider = StackBufferProvider(15) :> IScratchProvider
    use buf1 = provider.GetBuffer(5)
    use buf2 = provider.GetBuffer(10)
    Assert.Equal(0, buf1.Segment.Offset)
    Assert.Equal(5, buf2.Segment.Offset)
