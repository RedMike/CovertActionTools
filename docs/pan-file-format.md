# PAN file format

A PAN file is a self-contained animation. It bundles a set of images together with a control program
that describes how those images should be drawn, moved, and sequenced over time to produce an animated
scene. These are used for all in-game animations such as cutscenes, building interiors, and character
actions.

## How animations work

An animation is driven by two layers of control: **instructions** and **steps**.

**Instructions** are the top-level program. They run on a stack-based virtual machine with a single
execution head that advances through the instruction list. Instructions set up and tear down sprites,
wait for a number of frames (during which sprites simulate), trigger audio, stamp persistent copies of
sprites, and control flow with jumps and conditionals. The instruction VM also has registers that can
be read/written, which allows the game engine to pass values into the animation at runtime (e.g. to
select a character appearance).

**Sprites** are the individual animated elements within a scene. Each sprite is created by a
`SetupSprite` instruction, which assigns it a starting position, a follow target (another sprite whose
position it offsets from), and a pointer to a **step sequence**. Once created, a sprite runs its own
step sequence independently every frame while the instruction VM is waiting.

**Steps** are the per-sprite programs. They are much simpler than instructions: no stack, no registers,
just a linear sequence with counter-based looping. Steps tell a sprite to draw a specific image frame,
move to an absolute or relative position, loop back, pause, or stop. A step sequence typically looks
like a loop that cycles through image frames with relative movement each frame, creating the
appearance of motion.

A single animation frame works like this:

1. The instruction VM runs until it hits a `WaitForFrames` instruction
2. For each frame of waiting, every active sprite advances through its step sequence
3. Each sprite that hits a `DrawFrame` step emits a visible image at its current position
4. After all wait frames are consumed, the instruction VM resumes

**Stamping** is a mechanism where a sprite's current image and position are saved as a persistent
drawn image that remains on screen for the rest of the animation, even after the sprite moves or is
removed. This is used for effects like leaving footprints or building up a scene piece by piece.

**Background types** determine how the screen is initialised before the animation begins:
* `PreviousAnimation` (0x00) -- relies on a previous animation to have set up the screen content
* `ClearToImage` (0x01) -- draws the first image in the file as a full background before anything else
* `ClearToColor` (0x02) -- clears the screen to a solid colour before anything else

## Images

Each PAN file contains a set of embedded images. These are indexed through a 250-entry table that maps
image IDs (used in control data) to sequential image indices (the order they appear in the file). Gaps
in the table (entries with value 0x0000) mean that ID is unused. The maximum number of images is 250.

For `ClearToImage` animations, there is an additional background image stored before the index table
that is not part of the normal image IDs (it uses index -1 internally).

Each entry in the index table also has an associated u16 value. The purpose of this value is not fully
clear; it may be editor-related metadata (e.g. grid position for display in an authoring tool). It has
no known effect on playback. **Note: this value has never been observed to affect runtime behaviour.**

Images are stored sequentially in the file using the standard shared image format, padded to 2-byte
alignment.

## Colour mapping

Each PAN file includes a 15-byte colour mapping table (for colour indices 1-15). This allows the
animation to remap colours across all its images. In practice, no game files use non-identity mappings,
but the engine supports it.

## Binary file layout

The file is structured as follows:

| Offset | Size | Description |
|--------|------|-------------|
| 0x00 | 4 | Magic bytes: `PANI` (0x50 0x41 0x4E 0x49) |
| 0x04 | 5 | Tag: typically 0x03 0x01 0x01 0x00 0x03 (RRT files differ) |
| 0x09 | 15 | Colour mapping for indices 1-15 |
| 0x18 | 5 | Padding (all zeros) |
| 0x1D | 2 | Bounding width (u16, actual width - 1) |
| 0x1F | 2 | Bounding height (u16, actual height - 1) |
| 0x21 | 2 | Global frame skip (u16, 1 = normal, higher values skip frames) |
| 0x23 | 1 | Background type (0x00, 0x01, or 0x02) |

What follows depends on the background type:

**If ClearToImage (0x01):** one image is stored here (the background), 2-byte aligned, followed by the
index table.

**If ClearToColor (0x02):** two bytes follow -- the clear colour and an unknown byte -- then the index
table.

**If PreviousAnimation (0x00):** the index table follows immediately.

| Offset | Size | Description |
|--------|------|-------------|
| varies | 500 | Image index table: 250 x u16 entries |
| varies | varies | Sequential image data, each 2-byte aligned |
| varies | 2 | Data section size (u16, in units of 16 bytes) |
| varies | varies | Data section (instructions + steps), padded to 16-byte alignment |

## Data section

The data section contains two sub-sections stored contiguously: **instructions** followed by **steps**.
There is no explicit separator; the instruction sub-section ends at the last `End` (0x14) opcode, and
the step sub-section begins immediately after.

All jump targets and step pointers within the data section are absolute byte offsets from the start of
the data section.

### Instructions

Instructions are opcodes for a stack-based VM. Most opcodes that consume values are preceded by
`PushToStack` instructions that place those values on the stack. The VM has a value stack, indexed
registers, and a single instruction pointer.

| Byte | Name | Encoding | Description |
|------|------|----------|-------------|
| 0x00 | SetupSprite | 0x00 | Pops 7 values: step pointer, sprite index, follow index, X, Y, frame skip, flags. Creates or resets a sprite. |
| 0x01 | RemoveSprite | 0x01 | Pops 1 value: sprite index. Deactivates the sprite. |
| 0x02 | WaitForFrames | 0x02 | Pops 1 value: frame count. Pauses the instruction VM for that many frames while sprites simulate. A value of 0 runs one sprite update without advancing the frame counter. |
| 0x03 | TriggerAudio | 0x03 | Pops 1 value: audio index from the engine's global audio table. |
| 0x04 | StampSprite | 0x04 | Pops 1 value: sprite index. After the current wait completes, saves a persistent copy of the sprite's image and position. |
| 0x05 0x00 XX XX | PushToStack | 4 bytes | Pushes a signed 16-bit value (little-endian XX XX) onto the stack. |
| 0x05 0x01 XX XX | PushRegisterToStack | 4 bytes | Pushes the value of register XX XX onto the stack. |
| 0x06 XX XX | PopStackToRegister | 3 bytes | Pops the top stack value into register XX XX. Register -1 is a discard (pop without storing). |
| 0x07 | PushCopyOfStackValue | 0x07 | Duplicates the top stack value. |
| 0x08 | CompareEqual | 0x08 | Pops 2 values, pushes 1 if equal, 0 otherwise. |
| 0x09 | CompareNotEqual | 0x09 | Pops 2 values, pushes 1 if not equal, 0 otherwise. |
| 0x0A | CompareGreaterThan | 0x0A | Pops 2 values, pushes 1 if first > second, 0 otherwise. |
| 0x0B | CompareLessThan | 0x0B | Pops 2 values, pushes 1 if first < second, 0 otherwise. |
| 0x0C | CompareGreaterOrEqual | 0x0C | Pops 2 values, pushes 1 if first >= second, 0 otherwise. |
| 0x0D | CompareLessOrEqual | 0x0D | Pops 2 values, pushes 1 if first <= second, 0 otherwise. |
| 0x0E | Add | 0x0E | Pops 2 values, pushes their sum. |
| 0x0F | Subtract | 0x0F | Pops 2 values, pushes (first - second). |
| 0x10 | Multiply | 0x10 | Pops 2 values, pushes their product. |
| 0x11 | Divide | 0x11 | Pops 2 values, pushes (first / second). |
| 0x12 XX XX | ConditionalJump | 3 bytes | Pops 1 value; if non-zero, jumps to the absolute offset XX XX within the data section. |
| 0x13 XX XX | Jump | 3 bytes | Unconditionally jumps to the absolute offset XX XX within the data section. |
| 0x14 | End | 0x14 | Ends the animation. Acts as a perpetual WaitForFrames 1, keeping sprites alive and drawing. |
| 0x15 | EndImmediate | 0x15 | Ends the animation immediately with no further frame updates. |

For opcodes that pop values (SetupSprite, RemoveSprite, WaitForFrames, comparisons, arithmetic, etc.),
the values are always placed on the stack by preceding `PushToStack` instructions. The comparison and
arithmetic opcodes pop in stack order: the second-pushed value is treated as the "first" operand.

### Steps

Steps are per-sprite instructions with a simpler format: a type byte followed by type-specific data.
Steps support counter-based looping but have no stack or registers.

| Byte | Name | Data | Description |
|------|------|------|-------------|
| 0x00 | DrawFrame | 1 byte (signed) | Draw the image at the given ID. -1 means wait one frame without drawing. This is the only step that consumes a frame. |
| 0x01 | MoveAbsolute | 4 bytes (2x s16 LE) | Set position to (X, Y). |
| 0x02 | MoveRelative | 4 bytes (2x s16 LE) | Offset position by (dX, dY). |
| 0x03 | SetFrameSkip | 2 bytes (s16 LE) | Set the sprite's frame skip value. **Note: the exact mapping of this value to behaviour in the original engine is not fully clear; it has never been observed with non-standard values in game files.** |
| 0x04 | SetFrameAdjustment | 2 bytes (s16 LE) | Related to frame skip timing. **Note: relationship to SetFrameSkip is not fully clear; never observed in game files.** |
| 0x05 | PushCounter | 2 bytes (s16 LE) | Push a loop count. If a counter is already active, the current one is pushed to a counter stack and the new one becomes active. |
| 0x06 | JumpIfCounter | 2 bytes (u16 LE) | If the active counter > 0, decrement it and jump to the absolute offset. If it reaches 0 and the counter stack is non-empty, pop the previous counter. |
| 0x07 | Restart | none | Reset the sprite to its original step pointer and original position. Clears counter state. |
| 0x08 | Loop | none | Reset the sprite to its original step pointer without resetting position. Clears counter state. |
| 0x09 | Pause | none | Stop simulating but continue drawing the last frame. The sprite stays visible but frozen. |
| 0x0A | Stop | none | Stop simulating and drawing. The sprite becomes inactive. |

### Sprite following

A sprite can be set to "follow" another sprite via the follow index in `SetupSprite`. When following,
the sprite's drawn position is calculated by chaining through follow references: each sprite's position
is added to its follow target's position, recursively. This allows sprites to be attached to other
sprites (e.g. a character's arm following the body). The initial position set by `SetupSprite` is
relative to the followed sprite, and subsequent `MoveAbsolute`/`MoveRelative` steps continue to be
offsets in the chain.

### Global frame skip

The global frame skip value in the header affects how fast the animation plays. A value of 1 means
normal playback. Higher values (3 or 4, seen in building animations) cause frames to be skipped,
effectively speeding up the animation. This is separate from the per-sprite frame skip set via
`SetFrameSkip` steps.

### Registers and game engine integration

The instruction VM has numbered registers that persist across the animation. The game engine can set
register values before or during playback to control animation behaviour at runtime -- for example,
selecting which character sprite to display based on game state. Registers are read with
`PushRegisterToStack` and written with `PopStackToRegister`. Register -1 is a special discard target
that pops without storing.
