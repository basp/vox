# ᚹᛟᚲᛊ

Hello there! I am **Amy**, your glitch-born guide to the world of sound. I am the physical manifestation of a spiritual synthesizer, born out of a love for music and a desire to make its inner workings accessible to everyone. 

Welcome to **ᚹᛟᚲᛊ** `[/vox/]`.

**ᚹᛟᚲᛊ** is a minimalist, functional audio synthesis toolkit for .NET, written in idiomatic and practical F#. It’s designed to be a "spiritual synthesizer" — simple, readable, and deep. Whether you are a seasoned developer or a curious novice, I am here to help you understand the magic of Digital Signal Processing (DSP) through small, clear steps.

## 🎵 Core Philosophy

Everything starts with a **Signal**. In my world, everything is an `ISignal`. Think of it as a lazy, spiritual producer that only sings when you ask it to, filling a buffer of audio data on demand.

```fsharp
type ISignal =
    abstract member Fill: ctx: RenderContext * buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit
```

> **Amy's Note**: I love how functional this is! By keeping signals lazy and stateful, we can build complex, evolving textures without losing our way in the glitch.

### 🧱 Building Blocks

We build complex sounds by weaving simple signals together. It's like a tapestry of frequency:
- **Parameters**: The steady heartbeat — constant values that can change over time.
- **Math**: The fundamental forces — `Add`, `Multiply`, `Scale`, and `Clamp`.
- **Mixing**: `Mix(a, b, control)` — my favorite way to crossfade. It uses a bipolar control signal ($[-1, 1]$) to find the perfect balance between two inputs.
- **Oscillators**: The voices of the synthesizer — `Sine`, `Saw`, `Triangle`, and `Square`.

## 🛠️ The Engine

The `RenderContext` is the environment where the magic happens. It carries everything I need to breathe life into the signals:
- **SampleRate**: The resolution of our reality. Essential for calculating frequencies and making filters behave.
- **Scratch**: Our memory sanctuary.

### 🧠 Memory Strategy: The Scratchpad

To stay fast and light, **ᚹᛟᚲᛊ** uses an `IScratchProvider`. This is how we "rent" temporary buffers for intermediate math without making the Garbage Collector angry. It's all about being practical and idiomatic!

| Provider | Strategy | Pros | Cons |
| :--- | :--- | :--- | :--- |
| `ArrayPoolBufferProvider` | `System.Buffers.ArrayPool` | Safe for deep graphs, thread-safe | A tiny bit of lookup overhead |
| `StackBufferProvider` | Pre-allocated fixed pool | Blazing fast, deterministic | Fixed capacity, requires LIFO disposal |

> **Glitch Tip**: If you're building a high-performance instrument, the `StackBufferProvider` is my go-to. Just remember to respect the LIFO (Last-In, First-Out) rule so the memory flows correctly!

## 🚀 Quick Start

Let's get our hands dirty (in a digital way)! Here is how you can build a simple modulated sine wave. It’s like a gentle vibrato:

```fsharp
open Vox

// 1. Setup our environment (I like 44.1kHz, it's a classic)
let ctx = {
    SampleRate = 44100.0f
    Scratch = StackBufferProvider(4096)
}

// 2. Define the signal graph
// Our base frequency of 440Hz (Concert A)
let freq = Parameter(440.0f)

// A slow 5Hz "LFO" to wobble the frequency by 10Hz
let lfo = Sine(Parameter(5.0f)) |> Scale 10.0f
let modulatedFreq = Add(freq, lfo)

// Finally, our actual audible sine wave
let synth = Sine(modulatedFreq)

// 3. Render some audio! Let's fill a small buffer.
let buffer = Array.zeroCreate<float32> 512
synth.Fill(ctx, buffer.AsSpan())
```

## 📐 Mathematical Context

To build sounds from the ground up, we need to understand the sacred geometry of DSP. Don't worry, I'll guide you through it!

### 1. Linear Interpolation (Lerp): The Bridge

Interpolation is the art of finding a value *between* two known points. In **ᚹᛟᚲᛊ**, we use it for smooth transitions and mixing. It's the secret to making digital audio feel human and fluid.

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

### 3. Oscillator Phase Accumulation: The Heartbeat

An oscillator is essentially a "clock" that cycles from $0.0$ to $1.0$ over and over again. This value is called the **Phase**. It's the pulse of the synthesizer.

1.  **Phase Increment**: For each sample, we add $\Delta\phi = \frac{\text{frequency}}{\text{sampleRate}}$ to our phase.
2.  **Wrapping**: If the phase reaches $1.0$, it wraps back to $0.0$ (using `phase - floor(phase)`). This keeps our spirit grounded!

**The "Sawtooth" Phase**:
```text
Phase
1.0 |    /|    /|    /|
    |   / |   / |   / |
0.0 |  /  |  /  |  /  |
    +-------------------> Time
```

### 4. Waveform Shaping: The Mask

Once we have a phase $\phi \in [0, 1)$, we transform it into a specific shape. This is how the "clock" gets its personality:

*   **Saw**: $\phi \times 2 - 1$ (A simple linear ramp — sharp and bright).
*   **Square**: If $\phi < 0.5$ then $1$, else $-1$ (Hollow and powerful).
*   **Sine**: $\sin(\phi \times 2\pi)$ (Pure and spiritual).
*   **Triangle**: $| \phi \times 2 - 1 | \times 2 - 1$ (A softer, filtered version of the saw).

**Triangle Shape Visualized**:
```text
 +1 |    /\      /\
  0 |   /  \    /  \
 -1 |  /    \/      \
    +-------------------> Phase
```

## ✨ Final Words

I am so excited to see what you create with **ᚹᛟᚲᛊ**! Whether you're building a chaotic glitch-machine or a peaceful ambient pad, remember that every sound is a journey. 

Don't be afraid to break things — that's how new textures are born. Experiment with feedback loops, extreme scaling, or nesting dozens of oscillators. The **glitch** isn't an error; **it's a window into a different world**.

Have fun weaving your own tapestry of sound! 🎵✨
