# ᚹᛟᚲᛊ
Hello there! I am **Amy**, your glitch-born guide to the world of sound. I am 
the physical manifestation of a synthesizer, born out of a love for music and 
a desire to make its inner workings accessible to everyone: welcome to **ᚹᛟᚲᛊ** 
`[/vox/]`.

**ᚹᛟᚲᛊ** is a minimalist, functional audio synthesis toolkit for **.NET**, 
written in idiomatic and practical **F#**. It’s designed to be a *spiritual 
synthesizer* — simple, readable, and deep. Whether you are a seasoned developer 
or a curious novice, I am here to help you to understand the magic of Digital 
Signal Processing (DSP) through small, clear steps.

## 🎵 The Sacred Pulse: Core Philosophy
Everything starts with a **Signal**. In my world, everything is an `ISignal`. 
Think of it as a lazy, spiritual producer that only sings when you ask it to, 
filling a **buffer** of audio data on demand.

```fsharp
type ISignal =
    abstract member Fill: ctx: RenderContext * buffer: Span<float32> -> unit
    abstract member Reset: unit -> unit
```

> **Amy's Note**: I love how functional this is! By keeping signals lazy and stateful, we can build complex, evolving textures without losing our way in the glitch.

### ✨ The Lexicon of the Glitch
Before we build, let's align our frequencies with some sacred terminology:
- **Bipolar**: A signal that swings both ways ($-1.0$ to $1.0$). Most audio is bipolar, like a pendulum of pressure.
- **Unipolar**: A signal that stays in the light ($0.0$ to $1.0$). Perfect for controlling volume or mix amounts.
- **Phase**: Our internal clock ($\phi$). It’s the journey from start to finish before we wrap back and begin again.
- **The Buffer**: A temporary vessel where we store our sonic manifestations.
### 🧱 Building Blocks
We build complex sounds by weaving simple signals together. It's like a tapestry of frequencies:
- **Constants**: Static values that are suprisingly useful in DSP.
- **Parameters**: The steady heartbeat — values that can change over time.
- **Math**: The fundamental forces — `Add`, `Multiply`, `Scale`, and `Clamp`.
- **Mixing**: `Mix(a, b, control)` — my favorite way to crossfade. It uses a bipolar control signal ($[-1, 1]$) to find the perfect balance between two inputs.
- **Oscillators**: The voices of the synthesizer — `Sine`, `Saw`, `Triangle`, and `Square`.

## 🛠️ The Heart of the Machine: The Engine
The `RenderContext` is the environment where the magic happens. It carries everything I need to breathe life into the signals:
- **SampleRate**: The resolution of our reality. Essential for calculating frequencies and making filters behave.
- **Scratch**: Our memory sanctuary.

### 🧠 Memory Strategy: The Scratchpad
To stay fast and light, **ᚹᛟᚲᛊ** uses an `IScratchProvider`. This is how we *rent* temporary buffers for intermediate math **without making the garbage collector angry**. It's all about being **practical and idiomatic**!

| Provider | Strategy | Pros | Cons |
| :--- | :--- | :--- | :--- |
| `ArrayPoolBufferProvider` | `System.Buffers.ArrayPool` | Safe for deep graphs, thread-safe | A tiny bit of lookup overhead |
| `StackBufferProvider` | Pre-allocated fixed pool | Blazing fast, deterministic | Fixed capacity, requires LIFO disposal |

> **Glitch Tip**: If you're building a high-performance instrument, the `StackBufferProvider` is my go-to. Just remember to respect the LIFO (Last-In, First-Out) rule so the memory flows correctly!

## 🚀 Quick Start: Your First Manifestation
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

## 🌌 Advanced Textures
One of my favorite things about **ᚹᛟᚲᛊ** is how easily we can nest signals to create deep, spiritual textures. Let's look at some *spicier* recipes!

### 1. Frequency Modulation (FM) Synthesis
FM is where the magic really happens. We use one oscillator to *wiggle* another at very high speeds, creating complex sidebands and metallic tones.

```fsharp
// A "Carrier" at 100Hz, modulated by a "Modulator" at 150Hz
let modulatorFreq = Parameter(150.0f)
let modulationAmount = Parameter(200.0f)

// The modulator's output is scaled by the amount and added to the carrier frequency
let modulator = Sine(modulatorFreq) |> Multiply modulationAmount
let carrierFreq = Add(Parameter(100.0f), modulator)

let fmSynth = Sine(carrierFreq)
```

> **Amy's Note**: FM is the soul of 80s digital synths! Experiment with different frequency ratios to find those *glassy* or *gritty* sweet spots.

### 2. The Glitch Drone
We can layer multiple oscillators and crossfade between them using a slow control signal. This creates an evolving, breathing soundscape.

```fsharp
// Two different voices
let voiceA = Saw(Parameter(55.0f))   // A low, buzzy bass
let voiceB = Square(Parameter(110.0f)) // An octave higher, hollow and bright

// A slow triangle wave (0.1Hz) to sweep between them
let crossfadeController = Triangle(Parameter(0.1f))

// We mix them together! 
// When the controller is -1.0, we hear voiceA. When it's 1.0, we hear voiceB.
let drone = Mix(voiceA, voiceB, crossfadeController)

// Let's add a bit of "shimmer" by scaling the final output
let finalDrone = drone |> Scale 0.7f
```

> **Glitch Tip**: Try nesting a `Mix` *inside* another `Mix`! It's like a dream within a dream, but with more harmonics.

## 📐 The Sacred Geometry: Mathematical Context
To build sounds from the ground up, we need to understand the sacred geometry of DSP. Don't worry, I'll guide you through it!

### 1. Interpolation: The Bridges Between States
Interpolation is the art of finding a value *between* two known points. In **ᚹᛟᚲᛊ**, we use it for smooth transitions and mixing. It's the secret to making digital audio feel human and fluid.

#### A. Linear Interpolation (Lerp): The Straight Bridge
The most common path—a direct line from $a$ to $b$. Simple, fast, and honest.

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

#### B. Cosine Interpolation: The Gentle Curve
When a straight line feels too "mechanical," we can use a cosine curve to soften the start and end of the journey. It's like a dancer slowing down as they reach their mark.

**Formula**: 
$t_{cos} = \frac{1 - \cos(t \times \pi)}{2}$
$f(a, b, t) = a + (b - a) \times t_{cos}$

**Visualizing the Curve**:
```text
Value
  ^
b |           ..* (t=1.0)
  |         ./
  |      ..* (t=0.5)
  |    ./
a | *.. (t=0.0)
  +------------------> Time/Factor
```

#### C. Smoothstep: The Easing Path
A favorite in computer graphics and control signals. It uses a cubic polynomial ($3t^2 - 2t^3$) to provide a smooth "S-curve" that has zero velocity at both $0.0$ and $1.0$.

**Formula**: 
$t_{smooth} = t^2 \times (3 - 2t)$
$f(a, b, t) = a + (b - a) \times t_{smooth}$

> **Amy's Note**: Use **Lerp** for raw mixing (it's mathematically perfect for power-summing in some cases), **Cosine** for natural-feeling transitions, and **Smoothstep** when you want your control signals to "ease" in and out without any sudden jerks.

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

## 🧘 The Zen of ᚹᛟᚲᛊ
In my world, there are a few principles that keep the glitches beautiful and the signals pure:
- **Explicit is better than implicit.** (Let the signal graph tell the story.)
- **Simple is better than complex.** (A single sine wave can be a universe.)
- **Complex is better than complicated.** (FM synthesis is complex; messy code is complicated.)
- **Flat is better than nested...** (...unless it's a `Mix` inside a `Mix`!)
- **Readability counts.** (If I can't read your signal, I can't sing it.)
- **Errors should never pass silently...** (...unless they're beautiful glitches.)
- **In the face of ambiguity, refuse the temptation to guess.** (Trust the math!)

## ✨ Final Words
I am so excited to see what you create with **ᚹᛟᚲᛊ**! Whether you're building a chaotic glitch-machine or a peaceful ambient pad, remember that every sound is a journey. 

Don't be afraid to break things — that's how new textures are born. Experiment with feedback loops, extreme scaling, or nesting dozens of oscillators. The **glitch** isn't an error; **it's a window into a different world**.

Have fun weaving your own tapestry of sound! 🎵✨
