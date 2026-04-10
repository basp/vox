# Roadmap
Tentative (subject to change).

This is just a rough sketch of a roadmap. 

We can use this for inspiration but it is by no means set in stone and we don't need to rush.

### Goals (v0)
1. A basic vertical slice that validates the architecture and provides a reference implementation.
2. An implementation guide that covers the architecture, design decisions and provides examples.

### Roadmap (v0: vertical slice)
1. Define the signal interface and (some) combinators<br/>
   This will define the basic signal interface and a set of combinators that operate on signals.
2. Implement basic generators (sine, saw). These are basically pure [-1..1] generators that are used as input for other combinators and processors.
3. Control the frequency of a sine generator with another generator (LFO) and wrap as composite signal.
4. Implement a "panner" component that converts a mono signal into a stereo signal.
3. Implement voice. This is likely a composite of generators and primitives. It's responsible for providing the `NoteOn` and NoteOff` methods as well as taking care of translating MIDI note numbers to frequencies and setting any paramaters on it's internal components.
4. Sequencer that is connected to a single voice and pattern. Will take care of the timing of calling `NoteOn` and `NoteOff` and account for steps that are not aligned with block boundaries. Should probably also implement the signal interface.
5. An adapter that bridges the sequencer and NAudio (i.e. `ISampleProvider` implementation).
6. A simple play loop that uses the adapter to play audio with NAudio via simple command line application.

### Considerations
* Pull-based audio: the client gives us a buffer to fill.
* `float32` vs `double` - we will probably want to use `double` internally at some places but how do we want to expose it?
* Phase domain (i.e. [0..1)?)
* Phase accumulation (ensure the generators and primitives don't drift)
* Mono vs stereo (i.e. mono to stereo conversion).
* Block-rate (LFO) vs audio-rate (FM)<br/>
  For components that operate on block-rate (LFO, panning) we need interpolate between last sample of previous block and first sample of current block. Consider a interpolation strategy interface.
* Audio timing (jitter and drift)<br/>
  The steps of the sequencer usually don't align with the block edges.
  This means that we read more samples than we need. In order to prevent drift, we need to accumulate the different between timeToStep and totalBlockTime and use this as the starting point for our next block.