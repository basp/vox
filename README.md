# ᚹᛟᚲᛊ

**ᚹᛟᚲᛊ** `[/vox/]` is a minimalist, functional audio synthesis toolkit for .NET, written in idiomatic F#. It’s designed to be a "spiritual synthesizer" — simple, readable, and accessible to everyone.

## 🎵 Core Philosophy

At the heart of **ᚹᛟᚲᛊ** is the **Signal**. Everything is an `ISignal`. A signal is a lazy, stateful producer that fills a buffer of audio data when requested.

```fsharp
type ISignal =
    abstract member Fill: ctx: RenderContext * buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit
```

### 🧱 Building Blocks

Complex sounds are built by composing simple signals together. 
- **Parameters**: Constant values that can change over time.
- **Math**: `Add`, `Multiply`, `Scale`, and `Clamp`.
- **Mixing**: `Mix(a, b, control)` — uses a bipolar control signal ($[-1, 1]$) to crossfade between two inputs.
- **Oscillators**: `Sine`, `Saw`, `Triangle`, and `Square`.

## 🛠️ The Engine

The `RenderContext` carries the environment required for DSP operations:
- **SampleRate**: Essential for calculating oscillator frequencies and filter coefficients.
- **Scratch**: The active memory strategy.

### 🧠 Memory Strategy

To keep things fast and modular, **ᚹᛟᚲᛊ** uses an `IScratchProvider`. This allows child signals to *rent* temporary buffers for intermediate calculations without constant allocations.

The table below shows the tradeoffs between each builtin provider.

| Provider | Strategy | Pros | Cons |
| :--- | :--- | :--- | :--- |
| `ArrayPoolBufferProvider` | `System.Buffers.ArrayPool` | Thread-safe, robust | Slight lookup overhead |
| `StackBufferProvider` | Pre-allocated fixed pool | Near-zero overhead, deterministic | Fixed capacity, requires LIFO disposal |

## 🚀 Quick Start

Here is how you can build a simple modulated sine wave:

```fsharp
open Vox

// 1. Setup our environment
let ctx = {
    SampleRate = 44100.0f
    Scratch = StackBufferProvider(4096)
}

// 2. Define the signal graph
let freq = Parameter(440.0f)
let lfo = Sine(Parameter(5.0f)) |> Scale 10.0f
let modulatedFreq = Add(freq, lfo)

let synth = Sine(modulatedFreq)

// 3. Render some audio!
let buffer = Array.zeroCreate<float32> 512
synth.Fill(ctx, buffer.AsSpan())
```

## 📐 Mathematical Context

To build sounds from scratch, we use a few fundamental DSP (Digital Signal Processing) building blocks. Here’s a bottom-up look at how they work.

### 1. Linear Interpolation (Lerp)

Interpolation is the art of finding a value *between* two known points. In **ᚹᛟᚲᛊ**, we use it for smooth transitions and mixing.

**Formula**: $f(a, b, t) = a + (b - a) \times t$

*   $a$: Starting value
*   $b$: Ending value
*   $t$: The "mix" factor, from $0.0$ to $1.0$

**Visualizing Lerp**:
```text
Value
  ^
b |            * (t=1.0)
  |          /
  |        * (t=0.5)
  |      /
a |    * (t=0.0)
  +------------------> Time/Factor
```

### 2. Signal Mapping (Bipolar to Unipolar)

In audio, signals are often **bipolar** (ranging from $-1.0$ to $1.0$). However, control parameters like "mix amount" or "volume" are often **unipolar** (ranging from $0.0$ to $1.0$).

In the `Mix` signal, we convert a bipolar control signal $c$ into a unipolar weight $w$:
$$w = (c + 1) \times 0.5$$

| Bipolar ($c$) | Unipolar ($w$) | Resulting Mix |
| :--- | :--- | :--- |
| $-1.0$ | $0.0$ | 100% Input A |
| $0.0$ | $0.5$ | 50% A, 50% B |
| $1.0$ | $1.0$ | 100% Input B |

### 3. Oscillator Phase Accumulation

An oscillator is essentially a "clock" that cycles from $0.0$ to $1.0$ over and over again. This value is called the **Phase**.

1.  **Phase Increment**: For each sample, we add $\Delta\phi = \frac{\text{frequency}}{\text{sampleRate}}$ to our phase.
2.  **Wrapping**: If the phase reaches $1.0$, it wraps back to $0.0$ (using `phase - floor(phase)`).

**The "Sawtooth" Phase**:
```text
Phase
1.0 |    /|    /|    /|
    |   / |   / |   / |
0.0 |  /  |  /  |  /  |
    +-------------------> Time
```

### 4. Waveform Shaping

Once we have a phase $\phi \in [0, 1)$, we transform it into a specific shape:

*   **Saw**: $\phi \times 2 - 1$ (A simple linear ramp from $-1$ to $1$).
*   **Square**: If $\phi < 0.5$ then $1$, else $-1$.
*   **Sine**: $\sin(\phi \times 2\pi)$.
*   **Triangle**: $| \phi \times 2 - 1 | \times 2 - 1$.

**Triangle Shape Visualized**:
```text
 +1 |    /\      /\
  0 |   /  \    /  \
 -1 |  /    \/      \
    +-------------------> Phase
```
