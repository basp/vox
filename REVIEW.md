### Code Review Findings: Audio DSP in F#

This document summarizes the findings and architectural decisions made during the review of `Sandbox.fsx`.

#### 1. The Core Issue: Heap Allocations in the Hot Path
The initial implementation of binary operations (like `Add` and `Multiply`) used `Array.zeroCreate` inside the `Fill` method.
- **Problem:** In real-time audio (the "hot path"), allocating memory hundreds of times per second triggers Garbage Collector (GC) pauses, causing audio "clicks" or "pops".
- **Solution:** Transition to a zero-allocation architecture using pre-allocated memory or a buffer pool.

#### 2. Scratch Buffer Architecture
To avoid allocations, we explored "scratch buffers"—temporary storage used during a single block of processing.
- **Decision:** We opted for `System.Buffers.ArrayPool<float32>.Shared`.
- **Why:** It's a battle-tested, thread-safe, framework-provided pool that handles memory reuse efficiently without requiring custom lifecycle management.
- **Best Practice:** Always use `Rent(length)` paired with `try/finally` and `Return(array)` to ensure buffers are never leaked, even if an exception occurs.

#### 3. Navigating `Span<T>` and `ref struct` Restrictions
Using `Span<float32>` for performance introduced several compiler challenges in F#:
- **No Closures:** `Span<T>` cannot be captured in a lambda/closure because it is a stack-only `ref struct`.
- **IL Restrictions:** `Span<T>` cannot be used as a generic type argument (e.g., `FSharpFunc<Span<float32>, unit>`).

#### 4. The Final Architectural Solution
We implemented a `BinaryOp` helper module that provides a balance between readability and performance.

```fsharp
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
```

**Key Decisions:**
- **Explicit Parameters:** We passed the signals `a` and `b` as direct parameters to the helper rather than capturing them in a lambda. This bypasses the closure restriction.
- **Inlining:** Marking the helper as `inline` and the operation as `[<InlineIfLambda>]` ensures the compiler optimizes the math operation into the loop, removing the overhead of function calls.
- **Slicing:** We used `rented.AsSpan(0, buffer.Length)` because `ArrayPool.Rent` often returns an array larger than requested. Slicing ensures all signals in the graph process the exact same number of samples.

#### 5. Conclusion
The resulting implementation in `Sandbox.fsx` is high-performance, allocation-free, and maintains a clean, readable API. It follows the "Zen of Python" principle by being explicit about memory management while using F#'s powerful inlining capabilities to keep the code DRY (Don't Repeat Yourself).
