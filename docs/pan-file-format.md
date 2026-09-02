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
sprites, and control flow with jumps, conditionals and subroutine calls. The VM also has registers that
can be read/written, which allows the game engine to pass values into the animation at runtime (e.g. to
select a character appearance) and read values back out afterwards.

**Sprites** are the individual animated elements within a scene. There are 50 sprite slots, numbered
1 to 50. Each sprite is created by a `SetupSprite` instruction, which assigns it a starting position, a
follow target (another sprite whose position it offsets from), a rate, a flag, and a pointer to a
**step sequence**. Once created, a sprite runs its own step sequence independently every frame while
the instruction VM is waiting.

**Steps** are the per-sprite programs. They are much simpler than instructions: no stack, no registers,
just a linear sequence with counter-based looping. Steps tell a sprite to draw a specific image, move
to an absolute or relative position, loop back, pause, or stop. A step sequence typically looks like a
loop that cycles through images with relative movement each frame, creating the appearance of motion.

### The frame loop

The engine keeps two pages for the animation: a **background page** and a **draw page**. The
background page is set up once from the background type (see below) and only changes when sprites are
stamped into it. The draw page is what is shown after each frame. A single frame works like this:

1. The instruction VM runs until it hits a `WaitForFrames` (or `End`) instruction, or until a pending
   wait consumes the frame
2. Every active sprite advances through its step sequence, in slot order, if its speed allows a step
   this frame
3. Sprites marked by `StampSprite` this frame are drawn into the background page
4. The background page is copied back over every area of the draw page that a sprite was drawn on in
   an earlier frame, which erases the old sprite positions
5. Every active sprite that has an image is drawn onto the draw page, in slot order, so higher slots
   draw over lower ones
6. Audio queued by `TriggerAudio` this frame is triggered, most recent first

Sprite images are drawn with colour 0 as transparent. The erase in step 4 is driven by tracking, per
row, the horizontal span each draw touched; a sprite set up with a non-zero flag is drawn without that
tracking, so its pixels are never erased on later frames (it only gets cleared where another sprite's
erased area overlaps it). Retail animations use the flag for static images that never move, such as
large background pieces.

**Stamping** draws a sprite into the background page at the end of the frame in which `StampSprite`
ran, after the sprite's steps for that frame. The stamp remains for the rest of the animation, even
after the sprite moves or is removed, and is left behind for a following animation that uses the
`PreviousAnimation` background type. This is used for effects like leaving footprints or building up a
scene piece by piece.

**Background types** determine how the background page is initialised before the animation begins:
* `PreviousAnimation` (0x00) -- keeps the background page as the previous animation left it, including
  its stamps but not its last sprite positions
* `ClearToImage` (0x01) -- draws the first image in the file, at the top-left, as the background
* `ClearToColor` (0x02) -- fills the background with a solid colour

**Timing**: the frame delay in the header is the minimum number of system timer ticks (18.2 per
second) each frame lasts. A value of 1 plays at up to 18 frames per second; building animations use 3
to 5, which slows them to 4-6 frames per second. Playback ends when the game's requested duration runs
out or a key is pressed, or immediately on `EndImmediate`.

## Images

Each PAN file contains a set of embedded images. These are indexed through a 250-entry table that maps
image IDs (used in `DrawFrame` steps) to sequential image indices (the order they appear in the file).
Gaps in the table (entries with value 0x0000) mean that ID is unused. The maximum number of images is
250, and the ID 255 is reserved to mean "no image".

For `ClearToImage` animations, there is an additional background image stored before the index table
that is not part of the normal image IDs (it uses index -1 internally).

Each entry in the index table also has an associated u16 value. It has no effect on playback and is
probably authoring metadata (e.g. grid position for display in an authoring tool).

Images are stored sequentially in the file, each padded to 2-byte alignment. The image format byte in
the header chooses their encoding:
* non-zero -- the standard shared image format (LZW compressed)
* zero -- raw: the same 3-word header (a flag word the engine ignores, width, height), followed by
  `height` rows of `width` bytes, one colour index per byte

## Colour block

Each PAN file normally carries a 17-byte colour block: 16 palette entries for colour indices 0-15 and a
border (overscan) colour. When the animation starts, entry 0 is overwritten with 0 and the block is
loaded into the palette, so every colour index on screen displays as the standard colour the entry
names and colour 0 always displays as black. The EGA and Tandy drivers write the border colour to
their overscan/border register; the MCGA and CGA drivers hand the block to their generic palette
routine. The game restores its own palette afterwards. Retail animations store 3 in entry 0 and use an
identity mapping except colour 5, which maps to 0.

## Binary file layout

| Offset | Size | Description |
|--------|------|-------------|
| 0x00 | 4 | Magic bytes: `PANI` (0x50 0x41 0x4E 0x49) |
| 0x04 | 1 | Version, must be 0x03 or the game refuses the file |
| 0x05 | 1 | Image format: 0x01 compressed, 0x00 raw |
| 0x06 | 1 | Colour block present: 0x00 no, otherwise yes |
| 0x07 | 1 | Colour block kind, only present when the flag is set: 0x00 colour block, 0x01 none, 0x02 palette block |
| 0x08 | 17 | Colour block (kind 0x00): colours 0-15 (entry 0 is overwritten with 0 at runtime), border colour |
| 0x19 | 2 | Position X (u16), default screen position of the animation |
| 0x1B | 2 | Position Y (u16) |
| 0x1D | 2 | Bounding width (u16, actual width - 1) |
| 0x1F | 2 | Bounding height (u16, actual height - 1) |
| 0x21 | 2 | Frame delay (u16, timer ticks per frame, 1 = full speed) |
| 0x23 | 1 | Background type (0x00, 0x01, or 0x02) |

The offsets from 0x19 shift when the colour block is absent or has a different size. The game passes
its own screen position when it starts an animation, which overrides the position in the file.

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

The game reads exactly the declared number of bytes into memory and ignores anything after them.

## Data section

The data section holds the instructions and the steps in one block. There is no section boundary:
execution starts at offset 0, and steps are wherever the step pointers pushed for `SetupSprite` point.
Bytes that are never reached are ignored, so files can carry dead code and padding. Retail files place
the instructions first and the steps after the last `End` opcode.

All jump targets and step pointers are absolute byte offsets from the start of the data section.

### Instructions

Instructions are opcodes for a stack-based VM with a 16-bit value stack, 51 registers (0-50) and a
single instruction pointer. Opcodes that consume values pop them from the stack; retail files push
them with `PushToStack` immediately before. Arithmetic is 16-bit and wraps.

| Byte | Name | Encoding | Description |
|------|------|----------|-------------|
| 0x00 | SetupSprite | 0x00 | Pops 7 values: step pointer, sprite index, follow index, X, Y, rate, flags (step pointer is pushed first). Sprite index -1 takes the first free slot (slot 50 if none is free); indices outside 1-50 are ignored. Resets the slot: position, speed (to the rate), counters and step pointer. |
| 0x01 | RemoveSprite | 0x01 | Pops 1 value: sprite index. Deactivates the sprite; other indices are ignored. |
| 0x02 | WaitForFrames | 0x02 | Pops 1 value: frame count. Pauses the instruction VM for exactly that many frames while sprites simulate. A value of 0 does nothing. |
| 0x03 | TriggerAudio | 0x03 | Pops 1 value: audio index from the engine's global audio table, queued until the end of the frame. |
| 0x04 | StampSprite | 0x04 | Pops 1 value: sprite index. Draws the sprite into the background page at the end of this frame instead of the draw page. |
| 0x05 0x00 XX XX | PushToStack | 4 bytes | Pushes a signed 16-bit value (little-endian XX XX) onto the stack. |
| 0x05 0x01 XX XX | PushRegisterToStack | 4 bytes | Pushes the value of register XX XX onto the stack. Any non-zero second byte selects this form. Registers outside 0-50 are not checked. |
| 0x06 XX XX | PopStackToRegister | 3 bytes | Pops the top stack value into register XX XX. Registers outside 0-50 (such as -1) discard the value. |
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
| 0x11 | Divide | 0x11 | Pops 2 values, pushes (first / second). Division by zero crashes the game. |
| 0x12 XX XX | ConditionalJump | 3 bytes | Pops 1 value; if non-zero, jumps to the absolute offset XX XX within the data section. |
| 0x13 XX XX | Jump | 3 bytes | Unconditionally jumps to the absolute offset XX XX within the data section. |
| 0x14 | End | 0x14 | Ends the frame without advancing, so every later frame only steps and draws sprites, until the game stops the animation. |
| 0x15 | EndImmediate | 0x15 | Ends the animation immediately; the current frame is not stepped or drawn. |
| 0x16 | Return | 0x16 | Pops an address and jumps to it. |
| 0x17 XX XX | Call | 3 bytes | Pushes the address of the next instruction and jumps to the absolute offset XX XX. |

The comparison and arithmetic opcodes pop in stack order: the second-pushed value is treated as the
"first" operand. Any other opcode byte hangs the game.

### Steps

Steps are per-sprite instructions with a simpler format: a type byte followed by type-specific data.
Steps support counter-based looping but have no stack or registers.

A sprite only steps on frames its speed allows: every frame the speed is added to a credit that starts
at 255, and when the credit exceeds 255 it is reduced by 255 and the sprite runs steps until one ends
the frame. The speed starts at the rate given to `SetupSprite`. A speed of 255 steps every frame, 128
steps roughly every other frame, and 0 never steps.

| Byte | Name | Data | Description |
|------|------|------|-------------|
| 0x00 | DrawFrame | 1 byte | Draw the image with the given ID from now on. 255 (-1) draws nothing. This is the only step that ends the frame. |
| 0x01 | MoveAbsolute | 4 bytes (2x s16 LE) | Set position to (X, Y). |
| 0x02 | MoveRelative | 4 bytes (2x s16 LE) | Offset position by (dX, dY). |
| 0x03 | SetSpeed | 2 bytes (s16 LE) | Set the sprite's speed. |
| 0x04 | AddSpeed | 2 bytes (s16 LE) | Add the value to the sprite's speed. |
| 0x05 | PushCounter | 2 bytes (s16 LE) | Push a loop counter onto the sprite's counter stack (10 deep). |
| 0x06 | JumpIfCounter | 2 bytes (u16 LE) | Decrement the top counter; if it is still non-zero jump to the absolute offset, otherwise pop it and continue. A counter of N runs the loop body N times. |
| 0x07 | Restart | none | Reset the sprite to its original step pointer, position and speed (back to its rate), clear the counters, and keep stepping. |
| 0x08 | Loop | none | Reset the sprite to its original step pointer, clear the counters, and keep stepping, without resetting position or speed. |
| 0x09 | Pause | none | Stop stepping but keep drawing the last image. The sprite stays visible but frozen. |
| 0x0A | Stop | none | Stop stepping and drawing. The sprite becomes inactive. |

A `JumpIfCounter` with no counter pushed, an eleventh `PushCounter`, or any other step byte leave the
engine in an undefined state.

### Sprite following

A sprite can be set to "follow" another sprite via the follow index in `SetupSprite`, where -1 means no
follow and 1-50 names a slot. A following sprite starts at offset (0, 0) rather than at its X, Y, and is
drawn at:

    followed sprite's own X, Y + this sprite's setup X, Y + this sprite's current offset

Following is a single level: the followed sprite's own position is used even if it follows something
itself. `MoveAbsolute` and `MoveRelative` on a following sprite change its offset within that chain,
and `Restart` puts the offset back to (0, 0). A following sprite has an undefined position while the
followed slot is inactive, as does a follow index of 0 or a value above 50.

### Registers and game engine integration

The instruction VM has 51 registers (0-50) that persist across the animation. The game engine can set
register values before playback to control animation behaviour at runtime -- for example, selecting
which character sprite to display based on game state -- and can read registers back after playback.
Registers are read with `PushRegisterToStack` and written with `PopStackToRegister`; writes to any
index outside 0-50 are discarded, which is how retail files pop a value to discard it (register -1).

## Features not used by retail animations

The engine supports the following, but none of the retail PAN files use them:

* **Raw images** (image format byte 0x00): rows of one byte per pixel instead of the compressed shared
  image format
* **Colour block variants**: no colour block (byte 0x06 = 0, the palette is left untouched), kind 0x01
  (no block, the last loaded block is applied again) and kind 0x02 (a 774-byte palette block whose
  first 17 bytes are applied like the colour block); retail files always use kind 0x00
* **Non-identity colour mappings** beyond colour 5, and a non-zero border colour
* **Position X/Y** other than 0 (the game overrides them anyway)
* **Call and Return** (0x17, 0x16): subroutines, and instructions placed after `End`
* **Sprite index -1** in `SetupSprite` to take the first free slot
* **Computed SetupSprite parameters**: any of the 7 values pushed from registers or arithmetic rather
  than literals
* **WaitForFrames 0** (no wait) and negative waits (which wrap and wait for about 65536 frames)
* **Moving sprites with a non-zero flag**, which leave their previous images behind
* **Stamping a hidden sprite** (image 255), which draws garbage
