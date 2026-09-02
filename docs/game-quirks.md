This is a list of quirks that the game engine has. It is not an exhaustive list, and in general they should
not matter much, but this may help explain some of the odd behaviours seen.

## Crime system

**Object teleportation:** Items can jump between participants without a valid event chain -- the game
simply moves objects to the receiver even if the sender does not have them. This is not a parsing issue;
the game data itself relies on this behaviour, and trying to enforce correct ownership actually causes
errors because the game expects the teleportation to happen occasionally.

**Unreachable events:** Some events defined in the game data files have item requirements that can never
be fulfilled. These events will never actually run during gameplay. They are most likely bugs in the
original data rather than intentional dead content.

**"Ultimate Plot" score bug:** There is a minimum total score threshold that must be reached for a crime
to count as "successful". The "Ultimate Plot" crime does not have enough total score across its events
to meet this threshold, which means the crime repeats on loss instead of being counted as a player
failure.

**Duplicate receive events:** Some paired events in the legacy data have two receive-side entries
instead of the expected send/receive pair. The parser works around this by falling back to matching on
participant IDs when the normal send/receive pairing fails.

**Specific event data bugs:**
* Crime 6, event 13 -- the bomber is receiving the photographs they are meant to give
* Crime 9, events 11/12 -- the bagman is receiving things they are meant to give
* Crime 10, event 11 -- the extractor is not receiving the escapee
* Crime 3 -- the activation order of participants is clearly wrong

**Inside contact hang:** The `IsInsideContact` flag restricts a participant to only spawn from allied
organisations. If misused (e.g. no valid allied organisation candidates exist), this can cause the game
startup to hang indefinitely.

## Participant location/organisation mapping

The participant mapping flags (a bitmask stored as a single byte) control how participants are grouped
by location and organisation. The behaviour is only partially understood:

* Bits 7 and 6 (0x80, 0x40) seem to put participants in the same organisation
* Bits 5 and 4 (0x20, 0x10) seem to put participants in the same location
* Bit 4 (0x10) sometimes also places participants in allied organisations
* Bit 0 (0x01) always places participants in allied organisations (marks inside contacts)
* Bits 2 and 3 (0x04, 0x08) have inconsistent effects across different crimes
* Bit 1 (0x02) seems to lead to the same location

The system may use a two-tier preferred/fallback approach, but this has not been confirmed. The
mastermind always has this byte set to 0.

## Colour and rendering

**Context-dependent colour replacement:** Colour index 5 behaves differently depending on the sprite
context. For character sprites (including the player), it gets replaced with a designated colour. For
map sprites, it stays as the original colour. Some other colour indices become transparent on map
sprites but get replaced on the player's large UI sprite.

**Item box highlighting:** Selectable item boxes in sprite sheets are highlighted by replacing colours
in the sprite data rather than overlaying a separate highlight sprite.

**Unused and duplicate sprites:** Most sprites on the sprite sheets are not used by the game. Bullets,
magazines, and grenades appear as duplicates in the sheet data.

## LZW compression

**Unused dictionary entry 0x100:** The game's LZW compression implementation allocates dictionary entry
0x100 but never actually uses it. This is a bug in the original compression code that must be
replicated for round-trip compatibility.

## Animation engine

**Unreachable bytes in the data section:** Retail PAN files carry bytes the engine never executes: a
second End opcode (0x14) after the real one, unconditional jumps (0x13) directly after another jump,
and in BRIEFING.PAN a whole step sequence no sprite points at. The engine only follows reachable
instructions and step pointers, so the parser ignores them and republished files are shorter.

**Slow building animations:** Building animations use frame delay values of 3 to 5 instead of
the standard 1. The value is the number of timer ticks each frame lasts, so these play at 4-6 frames
per second rather than 18. This is the only observed use of non-1 values.

**Fixed colour block:** Every PAN file in the game data carries the same 17-byte colour block: entry
0 is 3 (overwritten with 0 by the engine), colour 5 maps to 0, every other colour maps to itself, and
the border colour is 0. The engine supports arbitrary mappings, but shipped content never varies it.

## Text files

**Deduplication via empty entries:** All four text-based formats (TEXT.DTA, PLOT.TXT, PROSE.DTA,
CLUES.TXT) use a pattern where multiple prefixes can share the same message text. Empty entries (prefix
with no message body) inherit the text from the next non-empty entry. This is likely an artefact of the
authoring tools used to create the data.

**Ambiguous duplicate key in TEXT.DTA:** At least one key in TEXT.DTA appears as a genuine duplicate
(same prefix, different message text). Which message the game actually uses in this case is unclear.
