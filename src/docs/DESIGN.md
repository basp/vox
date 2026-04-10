 # Scratch Buffer Findings

## Unified Memory Strategy

To balance performance and modularity, the engine uses an `IScratchProvider` interface. This allows swapping between different memory allocation strategies without changing signal implementation.

- `IScratchBuffer`: A disposable wrapper that provides an `ArraySegment<float32>`.
- `IScratchProvider`: A service that grants temporary buffers of a requested length.

## Implementations

### ArrayPoolBufferProvider
- Uses `System.Buffers.ArrayPool<float32>`.
- Pros: Safe for any graph depth; thread-safe (if using `Shared`); robust.
- Cons: Slight overhead from thread-local lookups.

### StackBufferProvider
- Pre-allocates a fixed-size `float32[]` pool.
- Uses a simple "stack pointer" (`currentOffset`) for allocations.
- Pros: Extremely fast (near-zero overhead); deterministic memory usage.
- Cons: Fixed capacity; requires strict LIFO disposal order.

## Nested Signal Safety (LIFO)

In a modular signal graph (e.g., `Mix(Add(a, b), c, control)`), child signals may also require scratch buffers. 

- **Allocation**: Buffers are rented sequentially as the graph is traversed.
- **Helper Modules**: To simplify this, the engine uses `BinaryOp` and `TernaryOp` modules. They abstract the "rent-fill-apply-dispose" lifecycle using F#'s `use` keyword.
- **Disposal**: `use` ensures that `IDisposable` resources are cleaned up in reverse order of their declaration.
- **Result**: In the `StackBufferProvider`, the last rented buffer is always the first one released, moving the stack pointer back correctly and preventing data corruption or "leaks" within the pool.

## The Span Limitation Workaround

Since `Span<T>` is a `ref struct`, it cannot be stored in class fields or used as an interface return type. 
- **Solution**: `IScratchBuffer` returns an `ArraySegment<float32>`.
- **Usage**: Signals convert the segment to a `Span` using `.AsSpan()` only inside the `Fill` method, where it is stack-safe.

## RenderContext and Environment

The `RenderContext` carries environment data required for DSP operations:
- `Scratch`: The active memory strategy.
- `SampleRate`: Essential for calculating oscillator frequencies and filter coefficients.

## Mathematical Building Blocks

Complex operations are built from simple, inlined math:
- `Interpolate.lerp`: Standard linear interpolation $a + (b - a) \times t$.
- `Mix`: A bipolar control signal ($[-1, 1]$) is mapped to a unipolar weight ($w = (c + 1) \times 0.5$) for crossfading between two inputs $x$ and $y$: $x + (y - x) \times w$.
- `Oscillator`: Tracks a `phase` ($[0, 1)$) that increments by $\text{freq} / \text{sampleRate}$ each sample. The `Shape` method transforms this phase into a waveform (Sine, Saw, etc.).
