# Text-based file formats

The game uses four text-based data files that all follow a similar pattern: entries are delimited by
asterisks (`*`), each entry has a prefix that identifies it, and the message body follows on the next
line. All four formats use `\r\n` line endings throughout.

These files store the text content that the game displays during gameplay -- crime messages, mission
briefings, in-game prose, and investigation clues. They are separate from the binary data files that
define the structure and logic of crimes, missions, and other game systems.

All four formats support a **deduplication mechanism**: when multiple entries share the same message
text, the duplicates are written as empty entries (prefix only, no message body) immediately before the
entry that contains the actual text. During parsing, empty entries inherit the message from the next
non-empty entry that follows them.

---

## TEXT.DTA - Crime messages and game text strings

This file contains text strings used during crime events, including messages intercepted by wire taps,
airport surveillance reports, and other in-game notifications. Each string is associated with a type
that determines which variable replacements are available and how the string is used.

### String types

| Type | Prefix | Variables | Description |
|------|--------|-----------|-------------|
| CrimeMessage | `MSG` | `$SNDORG`, `$SNDLOC`, `$VICTIM`, `$OBJECT`, Dateline | Messages shown during crime events. Prefixed with crime ID. |
| SenderOrganisation | `SORG` | `$SNDORG` | Organisation name for the sender side of an event. |
| ReceiverOrganisation | `RORG` | `$RCVORG` | Organisation name for the receiver side of an event. |
| SenderLocation | `SLOC` | `$SNDLOC` | Location name for the sender side of an event. |
| ReceiverLocation | `RLOC` | `$RCVLOC` | Location name for the receiver side of an event. |
| Fluff | `FLUF` | none | Flavour text with no gameplay significance. |
| Alert | `ALRT` | none | Alert notifications. |
| AidingOrganisation | `AIDD` | `$HLPORG` | Organisation name for aiding references. |

### File format

The file begins with `*` and contains a sequence of entries, terminated by `*END\r\n<EOF>` (where
`<EOF>` is byte 0x1A).

Each entry has the form:

```
*<PREFIX><ID>\r\n
<MESSAGE>\r\n
```

**Prefix encoding:**

For crime messages, the prefix is `MSG` followed by a 2-digit crime ID and a 2-digit message ID
(e.g. `MSG0103` = crime 1, message 3).

For all other types, the prefix is the type's 4-letter code followed by a 2-digit ID
(e.g. `SORG05`, `RLOC12`).

**Deduplication:** when multiple prefixes share the same message, the duplicates appear as entries with
no message body, immediately before the entry that contains the text:

```
*MSG0103\r\n
*MSG0203\r\n
This is the shared message text.\r\n
```

In this example, both `MSG0103` and `MSG0203` resolve to the same message.

**Ordering:** entries are written sorted by type, then crime ID (for crime messages), then ID.

---

## PLOT.TXT - Mission set plot strings

This file contains the narrative text for mission briefings and outcomes. Each mission set (a sequence
of related crimes) has briefing text shown at the start, and success/failure messages shown after each
crime is resolved. The mission set definitions themselves are stored in the EXE, not in this file --
this file only contains the text strings referenced by those definitions.

### String types

| Type | ID range | Description |
|------|----------|-------------|
| Briefing | 0-4 | Initial briefing text shown before the mission set begins. |
| BriefingPreviousFailure | 0-5 | Briefing text when a previous crime in the set was failed. |
| Success | 0-4 | Text shown when a crime in the set is completed successfully. |
| Failure | 0-4 | Text shown when a crime in the set is failed. |

### File format

The file begins with `*` and contains a sequence of entries, terminated by `\r\n<EOF>` (byte 0x1A).
Unlike the other text formats, there is no `*END` marker.

Each entry has the form:

```
*<PREFIX>\r\n
<MESSAGE>\r\n
```

**Prefix encoding:** `PL<XX><Y><Z>` (always 6 characters after the `*`)

| Character | Meaning |
|-----------|---------|
| `XX` | Mission set ID (2 digits, zero-padded) |
| `Y` | Crime index within the set (0-2), or `9` for briefing messages |
| `Z` | Message type + number (1 hex digit) |

The `Z` character encodes both the string type and sequential number:

| Z value | Type | Message number |
|---------|------|----------------|
| 0-4 | Success (or Briefing if Y=9) | Z |
| 5-9 | Failure | Z - 5 |
| A-F | BriefingPreviousFailure | Z - 10 (i.e. A=0, B=1, ...) |

Examples:
* `PL03090` -- mission set 3, briefing, message 0
* `PL031A` -- mission set 3, crime index 1, previous failure briefing, message 0
* `PL0307` -- mission set 3, crime index 0, failure, message 2 (7 - 5 = 2)
* `PL0313` -- mission set 3, crime index 1, success, message 3

**Ordering:** entries are sorted by mission set ID, then briefings first, then by type, crime index,
and message number.

---

## PROSE.DTA - In-game prose messages

This file contains longer-form text shown during gameplay events such as capturing a suspect,
interrogating someone, being ambushed, or receiving advice. Unlike the other text files which use
structured numeric prefixes, prose entries use human-readable keyword prefixes.

### Prose types

| Type | Prefix | Variants | Variables | Description |
|------|--------|----------|-----------|-------------|
| Advice | `advice` | `a`, `1`-`5` | `$RPLC` | Advice messages shown to the player. |
| CharacterCapture | `nice` | `0`, `1` | `$RPLC` | Text when capturing a non-mastermind character. |
| MastermindCapture | `nice` | `2` | `$RPLC` | Text when capturing the mastermind. |
| Lounge | `lounge` | none | none | Lounge/social messages. |
| CarFollowed | `followed` | none | none | Text when following a car. |
| CarFollowedEnd | `carcap` | none | none | Text when a car follow ends. |
| Interrogated | `grilled` | none | `$US`, `$NAME` | Interrogation dialogue. |
| Escape | `escape` | none | none | Text when a suspect escapes. |
| DoubleAgentAgree | `doublea` | none | none | Text when a double agent agrees to cooperate. |
| CharacterInterrogate | `inter` | `1`, `2` | none | Character-specific interrogation text. |
| Ambushed | `surprise` | none | none | Text when the player is ambushed. |
| AmbushedLost | `surprise` | `L` | `$RPLC` | Text when the player loses an ambush. |
| AmbushedWon | `surprise` | `W` | `$RPLC` | Text when the player wins an ambush. |

### File format

The file begins with `*` (preceded by any leading bytes until the first `*` is found) and contains a
sequence of entries, terminated by `*end\r\n<EOF>` (or `*END\r\n<EOF>`, byte 0x1A).

Each entry has the form:

```
*<PREFIX>\r\n
<MESSAGE>\r\n
```

**Prefix encoding:** the prefix is the keyword from the table above, optionally followed by a variant
suffix (e.g. `advicea`, `nice2`, `inter1`, `surpriseL`). Types without variants use the bare keyword
(e.g. `lounge`, `grilled`).

**Ordering:** entries are sorted by type, then by variant/secondary ID.

---

## CLUES.TXT - Investigation clue messages

This file contains the text shown when the player discovers clues through various investigation
methods (wire taps, surveillance, informants, etc.). Clues come in two varieties: generic clues that
apply to any crime based on the participant's clue type, and crime-specific clues that are tailored to
a particular crime and participant.

Changes to crime-specific clues should be coordinated with the corresponding CRIME data file, as both
reference the same participant IDs.

### Clue sources

The source determines the in-game investigation method that reveals the clue, affecting the display
image and "From:" line:

| Value | Source |
|-------|--------|
| 0 | Clandestine Photo (not used in legacy game data, but supported by engine) |
| 1 | Wire Tap |
| 2 | Covert Surveillance |
| 3 | File/Record Search |
| 4 | Local Informant |
| 5 | Interpol Database |
| 6 | Local Authorities |

### File format

The file begins with `*` and contains a sequence of entries, terminated by `*\r\n<EOF>` (byte 0x1A).

There are two entry formats depending on whether the clue is generic or crime-specific:

**Generic clues** (no crime ID):

```
*C<CLUETYPE><ID>\r\n
<SOURCE><MESSAGE>\r\n
```

Where `<CLUETYPE>` is a single digit (0-7, matching the ClueType enum from the crime file format),
`<ID>` is a single digit, and `<SOURCE>` is a single digit from the clue source table above.

**Crime-specific clues:**

```
*C<CRIMEID><PARTICIPANTID>\r\n
<SOURCE><CLUETYPE><MESSAGE>\r\n
```

Where `<CRIMEID>` is 2 digits (zero-padded), `<PARTICIPANTID>` is 2 digits, `<SOURCE>` is a single
digit, and `<CLUETYPE>` is a single digit. The participant ID also serves as the clue ID and links
back to the participant in the corresponding CRIME data file.

The parser distinguishes between the two formats by checking whether the 4th and 5th characters of the
prefix are `\r\n` (generic, 3-character prefix) or digits (crime-specific, 5-character prefix).

**Deduplication:** crime-specific clues can also appear as empty entries that inherit source, type, and
message from the next non-empty entry.

**Ordering:** generic clues come first, sorted by clue type then ID. Crime-specific clues follow,
sorted by crime ID then participant ID.

---

## Common patterns across all four formats

All four text-based formats share these characteristics:

* **Asterisk delimiters:** `*` marks the start of each entry's prefix and separates entries
* **Line endings:** all use `\r\n` (DOS-style)
* **EOF marker:** byte 0x1A marks the end of the file
* **Deduplication:** empty entries (prefix with no message) inherit the message text from the next
  non-empty entry, allowing multiple prefixes to share the same message without storing it multiple
  times
* **Message trimming:** one leading and one trailing `\r\n` are stripped from each message body during
  parsing
