using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CovertActionTools.Core.Models.Executables
{
    /// <summary>
    /// Mission set record from FINAL.EXE (74 bytes).
    /// Field interpretations are based on reverse engineering and may not be fully accurate.
    /// </summary>
    public class FinalMissionSetRecord
    {
        public const int RecordSize = 74;
        public const int NameLength = 25;
        public const int UnusedSlotCount = 2;
        public const int StringPointerCount = 16;
        public const int CrimeSlotCount = 7;
        public const int StringsPerSlot = 2;
        public const int TotalSlotStrings = CrimeSlotCount * StringsPerSlot; // 14

        /// <summary>Mission set name, null-padded to 25 bytes.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Org type eligibility bitmask at offset 0x19. The game reads a 16-bit word at
        /// record +0x18 (low byte is always 0x00 from name null-padding, high byte is this
        /// field). FINAL.EXE FUN_1100_0734 ANDs this word against the org's alliance
        /// membership field (WorldModel.Organisation.UniqueId high byte) to determine which
        /// criminal organisations can appear in this mission set.
        /// 4 bits used: 0x01, 0x02, 0x04, 0x08 — each bit represents an org type category.
        /// Value 0x0F (tutorial) matches all org types.
        /// TODO: decode the 4 bits into named org type categories.
        /// </summary>
        public byte OrgTypeMask { get; set; }

        /// <summary>First crime type ID (index into crime type names).</summary>
        public ushort Crime1Id { get; set; }

        /// <summary>Second crime type ID.</summary>
        public ushort Crime2Id { get; set; }

        /// <summary>Third crime type ID.</summary>
        public ushort Crime3Id { get; set; }

        /// <summary>Fourth crime type ID (slot 3). 0xFFFF = unused.</summary>
        public ushort Crime4Id { get; set; } = 0xFFFF;

        /// <summary>Fifth crime type ID (slot 4). 0xFFFF = unused.</summary>
        public ushort Crime5Id { get; set; } = 0xFFFF;

        /// <summary>Sixth crime type ID (slot 5). 0xFFFF = unused.</summary>
        public ushort Crime6Id { get; set; } = 0xFFFF;

        /// <summary>Crime slot 6 at offset +0x26 (2 bytes, always 0xFFFF). Not reachable by the RNG.</summary>
        public byte[] UnusedCrimeSlots { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Vestigial 8th crime slot at offset +0x28. Structurally occupies crime slot index 7
        /// in the crime ID array (+0x1A through +0x28). Records 1-9 have value 0x0001 (crime
        /// type "Theft"), records 0+10-15 have 0xFFFF (empty sentinel).
        /// FUN_1100_0906's crime selection has a "last crime guard": a slot is only selectable
        /// as the final crime once all earlier slots are used. The guard checks slot[N+1]==-1
        /// to identify the last slot. For slot 6, slot[7] is this FlagWord — so 0x0001 (not -1)
        /// means slot 6 is NOT the finale, and the hardcoded slot 7 with plot strings
        /// "Max Remington's apartment" / "a mattress full of cash" (at DS+0x235C/0x2376) would
        /// be the climax. 0xFFFF means slot 6 is the finale instead.
        /// However, the RNG call c169(6) only generates indices 0-5, so slots 6-7 are never
        /// reached at runtime. No per-record string pointers exist for slot 7 either (trailing
        /// pointer words 14-15 point to null padding).
        /// </summary>
        public ushort VestigialCrimeSlot7 { get; set; }

        /// <summary>
        /// 14 crime slot string pointers stored as 7 pairs of (victim, item/location).
        /// The game accesses slot N at record offset +0x2A + N*4 (2 pointers per slot).
        /// Index 2*N = victim/target string, index 2*N+1 = item/location string.
        /// Empty slots contain empty strings. 14 pointers + 2 trailing fill words = 16 total.
        /// </summary>
        public string[] SlotStrings { get; set; } = Array.Empty<string>();

        // SlotString pointers are computed at serialization time from string positions.

        /// <summary>Number of null padding bytes after this record's last string before the next record's strings.</summary>
        public int TrailingPadding { get; set; }

        public FinalMissionSetRecord Clone()
        {
            return new FinalMissionSetRecord
            {
                Name = Name,
                OrgTypeMask = OrgTypeMask,
                Crime1Id = Crime1Id,
                Crime2Id = Crime2Id,
                Crime3Id = Crime3Id,
                Crime4Id = Crime4Id,
                Crime5Id = Crime5Id,
                Crime6Id = Crime6Id,
                UnusedCrimeSlots = UnusedCrimeSlots.ToArray(),
                VestigialCrimeSlot7 = VestigialCrimeSlot7,
                SlotStrings = SlotStrings.Select(s => s).ToArray(),
                TrailingPadding = TrailingPadding
            };
        }

        public static FinalMissionSetRecord FromBytes(byte[] data, int offset, byte[] dataSegment)
        {
            var nameBytes = new byte[NameLength];
            Array.Copy(data, offset, nameBytes, 0, NameLength);
            var nameEnd = Array.IndexOf(nameBytes, (byte)0);
            if (nameEnd < 0) nameEnd = NameLength;
            var name = Encoding.ASCII.GetString(nameBytes, 0, nameEnd);

            // Extract 14 strings: 7 slots x 2 pointers each (victim + item/location)
            var slotStrings = new string[TotalSlotStrings];
            for (var i = 0; i < TotalSlotStrings; i++)
            {
                var ptr = BitConverter.ToUInt16(data, offset + 0x2A + i * 2);
                if (ptr > 0 && ptr < dataSegment.Length && dataSegment[ptr] != 0)
                {
                    var strEnd = ptr;
                    while (strEnd < dataSegment.Length && dataSegment[strEnd] != 0) strEnd++;
                    slotStrings[i] = Encoding.ASCII.GetString(dataSegment, ptr, strEnd - ptr);
                }
                else
                {
                    slotStrings[i] = string.Empty;
                }
            }

            return new FinalMissionSetRecord
            {
                Name = name,
                OrgTypeMask = data[offset + 0x19],
                Crime1Id = BitConverter.ToUInt16(data, offset + 0x1A),
                Crime2Id = BitConverter.ToUInt16(data, offset + 0x1C),
                Crime3Id = BitConverter.ToUInt16(data, offset + 0x1E),
                Crime4Id = BitConverter.ToUInt16(data, offset + 0x20),
                Crime5Id = BitConverter.ToUInt16(data, offset + 0x22),
                Crime6Id = BitConverter.ToUInt16(data, offset + 0x24),
                UnusedCrimeSlots = DataSegmentHelper.Slice(data, offset + 0x26, UnusedSlotCount),
                VestigialCrimeSlot7 = BitConverter.ToUInt16(data, offset + 0x28),
                SlotStrings = slotStrings
            };
        }

        /// <summary>
        /// Serializes the record to 74 bytes. slotStringOffsets must contain the DS-relative
        /// byte offset of each of the 7 slot strings (or the offset of the null padding area
        /// for empty slots). nullPadStart is the offset where null padding begins after the
        /// last real string.
        /// </summary>
        /// <summary>
        /// Serializes the record to 74 bytes. stringOffsets contains the DS-relative byte
        /// offset for each of the 14 slot strings. nullPadStart is the offset where null
        /// padding begins (for empty string pointers and trailing fill words).
        /// </summary>
        public byte[] ToBytes(int[] stringOffsets, int nullPadStart)
        {
            var result = new byte[RecordSize];
            var nameBytes = Encoding.ASCII.GetBytes(Name);
            Array.Copy(nameBytes, 0, result, 0, Math.Min(nameBytes.Length, NameLength));
            result[0x19] = OrgTypeMask;
            result[0x1A] = (byte)(Crime1Id & 0xFF); result[0x1B] = (byte)((Crime1Id >> 8) & 0xFF);
            result[0x1C] = (byte)(Crime2Id & 0xFF); result[0x1D] = (byte)((Crime2Id >> 8) & 0xFF);
            result[0x1E] = (byte)(Crime3Id & 0xFF); result[0x1F] = (byte)((Crime3Id >> 8) & 0xFF);
            result[0x20] = (byte)(Crime4Id & 0xFF); result[0x21] = (byte)((Crime4Id >> 8) & 0xFF);
            result[0x22] = (byte)(Crime5Id & 0xFF); result[0x23] = (byte)((Crime5Id >> 8) & 0xFF);
            result[0x24] = (byte)(Crime6Id & 0xFF); result[0x25] = (byte)((Crime6Id >> 8) & 0xFF);
            Array.Copy(UnusedCrimeSlots, 0, result, 0x26, Math.Min(UnusedCrimeSlots.Length, UnusedSlotCount));
            result[0x28] = (byte)(VestigialCrimeSlot7 & 0xFF); result[0x29] = (byte)((VestigialCrimeSlot7 >> 8) & 0xFF);

            // Build 16 pointer words: 14 string pointers + 2 trailing fill
            var words = new ushort[StringPointerCount];
            var fillPos = nullPadStart;
            for (var i = 0; i < TotalSlotStrings; i++)
            {
                if (i < stringOffsets.Length && !string.IsNullOrEmpty(SlotStrings[i]))
                {
                    words[i] = (ushort)stringOffsets[i];
                }
                else
                {
                    words[i] = (ushort)fillPos;
                    fillPos++;
                }
            }
            // Last 2 trailing fill words
            words[14] = (ushort)fillPos;
            words[15] = (ushort)(fillPos + 1);

            var ptrBytes = DataSegmentHelper.UInt16ArrayToBytes(words);
            Array.Copy(ptrBytes, 0, result, 0x2A, Math.Min(ptrBytes.Length, StringPointerCount * 2));
            return result;
        }
    }

    /// <summary>
    /// Per-org character appearance template (16 bytes), indexed by org unique ID (0-25).
    /// Used by the copyright screen: one org is selected and its portrait is rendered from
    /// this table, alongside 14 randomised portraits. FUN_1100_11b7 packs these fields into
    /// a portrait layer descriptor. FUN_1100_7afe renders the portrait by drawing 5 sprite
    /// layers from FACES (male) or FACESF (female) images, plus clothing drawn below the
    /// face. Skin and hair colours are applied via VGA palette colour replacement at draw time.
    /// There is no assigned portrait per mission or campaign — all in-game suspect portraits
    /// are generated at runtime.
    /// TODO: relate sprite indices to FACES/FACESF images in the editor UI.
    /// </summary>
    public class CopyrightOrgHeadRecord
    {
        public const int RecordSize = 16;

        /// <summary>Clothing sprite index (0-3). Drawn below the face by FUN_1100_1214.</summary>
        public ushort ClothingSprite { get; set; }

        /// <summary>Gender: 0 = female (FACESF), 1 = male (FACES).</summary>
        public ushort Gender { get; set; }

        /// <summary>Skin colour (0-1). Selects head shape variant and skin palette replacement.</summary>
        public ushort SkinColour { get; set; }

        /// <summary>Mouth sprite variant (1-8). Packed into bits 4-6 of the portrait descriptor.</summary>
        public ushort Mouth { get; set; }

        /// <summary>Nose sprite variant (1-8). Packed into bits 7-9 of the portrait descriptor.</summary>
        public ushort Nose { get; set; }

        /// <summary>Eyes sprite variant (1-8). Packed into bits 10-12 of the portrait descriptor.</summary>
        public ushort Eyes { get; set; }

        /// <summary>Hair sprite variant (1-8). Packed into bits 13-15 of the portrait descriptor.</summary>
        public ushort Hair { get; set; }

        /// <summary>Hair colour (0-3). Applied via palette colour replacement at draw time.</summary>
        public ushort HairColour { get; set; }

        public CopyrightOrgHeadRecord Clone()
        {
            return new CopyrightOrgHeadRecord
            {
                ClothingSprite = ClothingSprite,
                Gender = Gender,
                SkinColour = SkinColour,
                Mouth = Mouth,
                Nose = Nose,
                Eyes = Eyes,
                Hair = Hair,
                HairColour = HairColour
            };
        }

        public static CopyrightOrgHeadRecord FromBytes(byte[] data, int offset)
        {
            return new CopyrightOrgHeadRecord
            {
                ClothingSprite = BitConverter.ToUInt16(data, offset + 0x00),
                Gender = BitConverter.ToUInt16(data, offset + 0x02),
                SkinColour = BitConverter.ToUInt16(data, offset + 0x04),
                Mouth = BitConverter.ToUInt16(data, offset + 0x06),
                Nose = BitConverter.ToUInt16(data, offset + 0x08),
                Eyes = BitConverter.ToUInt16(data, offset + 0x0A),
                Hair = BitConverter.ToUInt16(data, offset + 0x0C),
                HairColour = BitConverter.ToUInt16(data, offset + 0x0E)
            };
        }

        public byte[] ToBytes()
        {
            var result = new byte[RecordSize];
            result[0x00] = (byte)(ClothingSprite & 0xFF); result[0x01] = (byte)((ClothingSprite >> 8) & 0xFF);
            result[0x02] = (byte)(Gender & 0xFF); result[0x03] = (byte)((Gender >> 8) & 0xFF);
            result[0x04] = (byte)(SkinColour & 0xFF); result[0x05] = (byte)((SkinColour >> 8) & 0xFF);
            result[0x06] = (byte)(Mouth & 0xFF); result[0x07] = (byte)((Mouth >> 8) & 0xFF);
            result[0x08] = (byte)(Nose & 0xFF); result[0x09] = (byte)((Nose >> 8) & 0xFF);
            result[0x0A] = (byte)(Eyes & 0xFF); result[0x0B] = (byte)((Eyes >> 8) & 0xFF);
            result[0x0C] = (byte)(Hair & 0xFF); result[0x0D] = (byte)((Hair >> 8) & 0xFF);
            result[0x0E] = (byte)(HairColour & 0xFF); result[0x0F] = (byte)((HairColour >> 8) & 0xFF);
            return result;
        }
    }

    /// <summary>
    /// Structured data segment for FINAL.EXE.
    /// Field boundaries and interpretations are based on reverse engineering and may not
    /// be fully accurate. Unknown regions are preserved as raw byte arrays.
    /// </summary>
    public class FinalDataSegment
    {
        /// <summary>DS paragraph value for FINAL.EXE.</summary>
        public const int DsParagraph = 0x10D8;

        #region Layout Constants (DS-relative offsets)
        private const int CopyrightOrgHeadOffset = 0x1CFC;    // 0x012A7C - 0x10D80
        private const int CopyrightOrgHeadCount = 26;
        private const int CopyrightOrgHeadRecordSize = 16;
        private const int CopyrightOrgHeadSize = CopyrightOrgHeadCount * CopyrightOrgHeadRecordSize; // 416
        private const int MissionSetsOffset = 0x1E9E;      // 0x012C1E - 0x10D80
        private const int MissionSetCount = 16;
        private const int CrimeTypesOffset = 0x2AFC;        // 0x01387C - 0x10D80
        private const int CrimeTypeCount = 13;
        private const int OrgsOffset = 0x2B80;              // 0x013900 - 0x10D80
        private const int OrgCount = 26;
        private const int CharNamePointerCount = 192;

        // PostMissionPreCrimeData sub-section layout (DS-relative offsets within original binary)
        private const int PlotFileStringsCount = 9;         // *PL0090, briefing.pan, plot.txt, victim, item, *PL000A, *PL000a, briefing.pan, plot.txt
        private const int CharCreationStringsCount = 2;     // "Max's code name is:" + difficulty menu
        private const int SkillNameCount = 5;               // Combat, Driving, Cryptography, Electronics, Stamina
        private const int TrainingScreenStringsCount = 8;   // training.pic, " ", " practice\n ", " training\n ", column header, Average, Good, Excellent, Awesome -> BUT the column header includes "Average", so actually the raw parse gives different counts
        private const int TrainingColorWordCount = 4;
        private const int TrainingPaletteRemapSize = 16;
        private const int CopyrightProtStringsCount = 2;    // "Max, I'm sure..." + " among these faces."
        private const int RastPortSize = 20;                // 20-byte RastPort block
        private const int RastPortConfigPointerSize = 2;    // 2-byte config pointer after RastPort
        private const int PlotFileBufferSize = 9;           // "*PL0000\0\0"

        // PostOrgPreCharNameData sub-section sizes (bytes, from binary investigation)
        private const int CareerReviewStringsByteSize = 203;     // 19 career review strings
        private const int CrimeOrgPointerTableSize = 78;        // 39 uint16 pointers (13 crime + 26 org, recomputed)
        private const int MissionEndStringsByteSize = 395;      // 24 mission end strings
        private const int CrimePointerCount = 13;
        private const int OrgPointerCount = 26;                 // includes PFO and M18
        private const int ScenePointersByteSize = 9;            // 1 null + 4 uint16 (skipped, recomputed)
        private const int SceneRecordCount = 21;                // 4 scenes x 5 variations + 1 all-masterminds
        private const int SceneRecordWords = 5;                 // words per record
        private const int SceneRecordsByteSize = SceneRecordCount * SceneRecordWords * 2; // 210
        private const int CluePhrasesSize = 544;               // 40 phrases, identical to TAC
        private const int MonthsSize = 48;                     // 12 months, identical to TAC
        private const int IntelHeadersSize = 134;              // ~13 strings
        private const int CluePhrasePointerTableSize = 80;     // 40 x uint16 (skipped, recomputed)
        private const int ClueCategoryDataSize = 48;          // 16 bit flags + 4x8 popcount lookups
        private const int MonthPointerTableSize = 24;          // 12 x uint16 (skipped, recomputed)
        private const int IntelMidPaddingSize = 3;
        private const int EvidenceRankPointerTableSize = 160;  // (skipped, recomputed)
        private const int SceneFilenameCount = 4;              // "lau", "off", "bch", "cas"
        private const int GameStateDataSize = 1242;            // TR section: DS:0x46A6-0x4B7F
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before the mission set string table.</summary>
        public byte[] PreStringTableData { get; set; } = Array.Empty<byte>();

        // Mission set slot strings are stored per-record in FinalMissionSetRecord.SlotStrings.
        // The string table is rebuilt from these in ToBytes().

        /// <summary>Data between string table and mission params.</summary>
        public byte[] PostStringTableData { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// 26 x 16-byte org appearance records, indexed by org unique ID (0-25).
        /// Each record defines a portrait template used by the copyright screen.
        /// FUN_1100_11b7 packs the fields into a portrait layer descriptor; FUN_1100_7afe
        /// renders layered character portraits from FACES/FACESF sprites.
        /// </summary>
        public CopyrightOrgHeadRecord[] CopyrightOrgHeads { get; set; } = Array.Empty<CopyrightOrgHeadRecord>();

        /// <summary>2-byte gap between params and mission set records.</summary>
        public byte[] Unknown1 { get; set; } = Array.Empty<byte>();

        /// <summary>16 mission set records (74 bytes each): name, crime IDs, flags, string pointers.</summary>
        public FinalMissionSetRecord[] MissionSets { get; set; } = Array.Empty<FinalMissionSetRecord>();

        #region PostMissionPreCrimeData sub-sections (in binary order)

        /// <summary>
        /// Plot file reference strings used during mission setup: tutorial plot ref (*PL0090),
        /// briefing.pan, plot.txt, slot 7 hardcoded victim/item strings, alternate plot refs.
        /// TODO: investigate *PL000A/*PL000a alternate plot ref logic (part of plot file model/parser weirdness).
        /// </summary>
        public string[] PlotFileStrings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Character creation strings: "Max's code name is:" and difficulty selection menu text.
        /// TODO: the difficulty menu string contains multiple menu options as one string (newline-separated).
        /// Investigate whether these should be split into individual option strings.
        /// </summary>
        public string[] CharacterCreationStrings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 5 skill names: Combat, Driving, Cryptography, Electronics, Stamina.
        /// Note: Stamina (index 4) is unused in the game normally.
        /// </summary>
        public string[] SkillNames { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Training screen UI strings: training.pic, formatting strings, skill rating labels.
        /// Does not include the column position header (stored separately in TrainingScreenColumnPositions).
        /// </summary>
        public string[] TrainingScreenStrings { get; set; } = Array.Empty<string>();

        /// <summary>5 x-coordinate values for laying out skill bar columns on the training screen.</summary>
        public byte[] TrainingScreenColumnXCoords { get; set; } = Array.Empty<byte>();

        /// <summary>4 color/mode words used by the training screen for skill bar rendering (one per skill display slot).</summary>
        public ushort[] TrainingScreenColorWords { get; set; } = Array.Empty<ushort>();

        /// <summary>16-byte VGA palette remap table for the training screen. Index = source color, value = dest color.</summary>
        public byte[] TrainingScreenPaletteRemap { get; set; } = Array.Empty<byte>();

        /// <summary>Trailing null byte after the palette remap table.</summary>
        public byte TrainingScreenRemapTerminator { get; set; }

        /// <summary>Copyright protection screen strings: "Max, I'm sure you recognize the head of\nthe " and " among these faces.\n".</summary>
        public string[] CopyrightProtectionStrings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Game progress strings: briefing templates, case chronology intro, MasterMind capture text,
        /// promotion dialogue, retirement text, continue/save/end menus, difficulty change menus.
        /// TODO: some strings contain multiple menu options as one newline-separated string.
        /// Investigate whether these should be split into individual option strings.
        /// </summary>
        public string[] GameProgressStrings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// RastPort graphics context block (20 bytes). Used as a display descriptor for the briefing panel.
        /// Fields: DataOffset, Page, OriginX, OriginY, ExtentX (319), ExtentY (199), Flag, MaxColor (15), BPP (4), Reserved.
        /// The first 2 bytes (DataOffset) overlap with the null terminator of the last GameProgressStrings entry.
        /// </summary>
        public ushort RastPortDataOffset { get; set; }
        public ushort RastPortPage { get; set; }
        public ushort RastPortOriginX { get; set; }
        public ushort RastPortOriginY { get; set; }
        public ushort RastPortExtentX { get; set; } = 319;
        public ushort RastPortExtentY { get; set; } = 199;
        public ushort RastPortFlag { get; set; }
        public ushort RastPortMaxColor { get; set; } = 15;
        public ushort RastPortBPP { get; set; } = 4;
        public ushort RastPortReserved { get; set; }

        // RastPort config pointer (2 bytes after the 20-byte block) is computed at serialization time.

        /// <summary>
        /// Writable plot file buffer (9 bytes). Initial value "*PL0000\0\0" — always overwritten at runtime
        /// before use. Preserved for binary roundtrip fidelity only; not shown in editor UI.
        /// </summary>
        public byte[] PlotFileBuffer { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Chronology format strings used to build case event text. Format tokens (" (", ") ", "/", "^"),
        /// event phrases ("sent message to", "You arrested", etc.), and status phrases ("Mission completed.",
        /// " went into hiding.", etc.). Accessed by unrecognized code in FUN_1100_1da2.
        /// Includes intentional empty strings used as format placeholders.
        /// </summary>
        public string[] ChronologyFormatStrings { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Time/date display template "HH:MM AM Mon DD" with fixed separator characters.
        /// Digits and month name are overwritten at runtime by FUN_1100_2c94; the fixed
        /// characters (":", " ", "M") are preserved from the template. Only the month+day
        /// portion is displayed in-game (e.g. "Jan 08"). FUN_2c94 also strcat's " 10" onto
        /// the end of this buffer at runtime, but it is never displayed.
        /// </summary>
        public string TimeTemplateBuffer { get; set; } = string.Empty;

        /// <summary>
        /// Efficiency report display strings: "Efficiency Report", status labels (At Large, ARRESTED, TURNED,
        /// CONFISCATED), EP score formatting, Double Agent text, Efficiency Rating, formatting delimiters.
        /// Contains 0x89 bytes whose purpose is unconfirmed. The EGRAPHIC text renderer stops and returns
        /// when it encounters any byte >= 0x80, but what happens after the return is not yet traced.
        /// The 0x89 may trigger a color change or re-render; in-game testing is needed to confirm.
        /// See scratch/exe-investigation/FINAL.efficiency-report-0x89.md for full investigation notes.
        /// </summary>
        public string[] EfficiencyReportStrings { get; set; } = Array.Empty<string>();

        #endregion

        /// <summary>13 crime type name strings.</summary>
        public string[] CrimeTypeNames { get; set; } = Array.Empty<string>();

        /// <summary>Original per-string byte sizes for CrimeTypeNames (prevents pointer drift).</summary>
        public int[] CrimeTypeNameByteSizes { get; set; } = Array.Empty<int>();

        /// <summary>Data between crime type names end and organisation names start.</summary>
        public byte[] Unknown2 { get; set; } = Array.Empty<byte>();

        /// <summary>24 organisation name strings.</summary>
        public string[] OrganisationNames { get; set; } = Array.Empty<string>();

        /// <summary>Original per-string byte sizes for OrganisationNames (prevents pointer drift).</summary>
        public int[] OrganisationNameByteSizes { get; set; } = Array.Empty<int>();

        #region PostOrgPreCharNameData sub-sections (in binary order)

        /// <summary>19 career review strings: "Career", "The Career of", case summary labels,
        /// arrest counts, EP formatting, MasterMinds Arrested heading.</summary>
        public string[] CareerReviewStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for CareerReviewStrings (prevents pointer drift).</summary>
        public int[] CareerReviewStringSizes { get; set; } = Array.Empty<int>();

        // Crime/org name pointer table (78 bytes = 13 crime + 26 org uint16 pointers) is
        // NOT stored — recomputed at serialization from CrimeTypeNames and OrganisationNames.

        /// <summary>24 mission end strings: gender.pic, Max, Remington, ARRESTED, scene codes
        /// ("lau","off","bch","cas"), 4 flavour texts, file refs (final4.cat, back.pic, etc.),
        /// filename fragments (dude, babe, .pic).</summary>
        public string[] MissionEndStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for MissionEndStrings (prevents pointer drift).</summary>
        public int[] MissionEndStringSizes { get; set; } = Array.Empty<int>();

        // 4 scene filename pointers (uint16 into MissionEndStrings for "lau","off","bch","cas")
        // are NOT stored — recomputed at serialization.

        /// <summary>
        /// Mission end scene selection table: 21 records x 5 uint16 words (105 values).
        /// 4 scene types (laundromat/office/beach/casino) x 5 score variations + 1 all-masterminds record.
        /// Word[0] = scene index (0-3), words[1-4] = sub-image numbers or 0xFFFF (unused).
        /// Combined with player gender to build filenames like "laudude1.pic" / "laubabe1.pic".
        /// </summary>
        public ushort[] MissionEndSceneRecords { get; set; } = Array.Empty<ushort>();

        // No gap between scene records and briefing strings — they are contiguous.

        /// <summary>Briefing intro strings: "Red Herring", region descriptions, mission intro,
        /// practice prompt, briefing.pan, 10.dta, crime0.dta, world0.dta file refs.</summary>
        public string[] BriefingStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for BriefingStrings.</summary>
        public int[] BriefingStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>Hall of Fame display strings: fame.dta file refs (x4), "Hall of Fame",
        /// "COVERT ACTION", score formatting labels. Contains 0x80+ control bytes.</summary>
        public string[] HallOfFameStrings { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for HallOfFameStrings.</summary>
        public int[] HallOfFameStringSizes { get; set; } = Array.Empty<int>();

        /// <summary>40 clue relationship phrases (" tied to ", " registered to ", etc.).
        /// Identical content to TAC/GAME/BUG EXEs.</summary>
        public string[] ClueRelationshipPhrases { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for clue phrase slots.</summary>
        public int[] CluePhraseSizes { get; set; } = Array.Empty<int>();

        /// <summary>12 month abbreviations: "Jan", "Feb", ... "Dec". Identical to TAC.</summary>
        public string[] MonthAbbreviations { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for month abbreviation slots.</summary>
        public int[] MonthSizes { get; set; } = Array.Empty<int>();

        /// <summary>Intel report headers and format fragments ("CODED MESSAGE:", "MEETING NOTES:", etc.).
        /// Same content as TAC.</summary>
        public string[] IntelHeaders { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for intel header slots.</summary>
        public int[] IntelHeaderSizes { get; set; } = Array.Empty<int>();

        /// <summary>
        /// 48-byte clue category and popcount lookup table. Identical across FINAL/TAC/GAME.
        /// Bytes 0-15: category bit flags per clue pair (values 1/2/4/8 = single-bit masks).
        /// Bytes 16-23: popcount(0-7) + 0 lookup.
        /// Bytes 24-31: popcount(0-7) + 1 lookup.
        /// Bytes 32-39: popcount(0-7) + 1 lookup (duplicate of 24-31).
        /// Bytes 40-47: popcount(0-7) + 2 lookup.
        /// TODO: investigate how the clue system uses this table — the bit flags likely map
        /// clue slots to evidence categories, and the popcount sub-tables count active categories
        /// for a given bitmask. Trace from GAME.EXE clue processing code to confirm.
        /// </summary>
        public byte[] ClueCategoryData { get; set; } = Array.Empty<byte>();

        /// <summary>3 bytes: zero padding between month pointer table and intel report texts.</summary>
        public byte[] IntelMidPadding { get; set; } = Array.Empty<byte>();

        /// <summary>Agent identification templates (~17 strings). Identical to TAC.
        /// Contains newline (0x0A) bytes.</summary>
        public string[] IntelReportTexts { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for intel report text slots.</summary>
        public int[] IntelReportTextSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 agent rank names: "Recruit", "Operative", ... "MasterMind". Identical to TAC.</summary>
        public string[] RankNames { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for rank name slots.</summary>
        public int[] RankNameSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 evidence type abbreviations: "CAR", "WPN", "ADR", "TKT", "MSG", "$", "$", "FCE".
        /// Identical to TAC.</summary>
        public string[] EvidenceTypeAbbreviations { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for evidence type slots.</summary>
        public int[] EvidenceTypeSizes { get; set; } = Array.Empty<int>();

        /// <summary>64 evidence item name templates: vehicles(8), weapons(8), streets(8),
        /// airlines(8), telecom(8), money(16), passports(8). Identical to TAC.</summary>
        public string[] EvidenceItemNames { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for evidence item slots.</summary>
        public int[] EvidenceItemSizes { get; set; } = Array.Empty<int>();

        /// <summary>8 investigation method names: "Clandestine Photo", ... "Local Authorities", "Clue".
        /// Identical to TAC.</summary>
        public string[] InvestigationMethods { get; set; } = Array.Empty<string>();
        /// <summary>Original byte sizes for investigation method slots.</summary>
        public int[] InvestigationMethodSizes { get; set; } = Array.Empty<int>();

        /// <summary>Remaining clue system data: UI text, template variables, file references,
        /// suspect labels, message templates. Preserved as raw blob (same pattern as TAC).</summary>
        public byte[] ClueSystemData { get; set; } = Array.Empty<byte>();

        #endregion

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and trailing data (gap before pointer table position).</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        // CharacterNamePointers are computed at serialization time from CharacterNames positions.

        /// <summary>
        /// Game state and file management data (1,242 bytes). Contains status labels, chronology
        /// phrases, save/load UI, difficulty suffixes, EXE chain refs, disk prompts, PANI markers,
        /// RastPort blocks, and palette data — interleaved strings and binary.
        /// TODO: split into typed sub-sections when the interleaved binary regions are understood.
        /// </summary>
        public byte[] GameStateData { get; set; } = Array.Empty<byte>();

        /// <summary>MSC overlay/error strings, game state buffers, C runtime internals, BSS.
        /// Not editable — preserved for binary roundtrip fidelity only.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static FinalDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new FinalDataSegment();

            // Parse mission set records first (needed to find string table boundaries)
            segment.MissionSets = new FinalMissionSetRecord[MissionSetCount];
            for (var i = 0; i < MissionSetCount; i++)
            {
                segment.MissionSets[i] = FinalMissionSetRecord.FromBytes(dataSegment, MissionSetsOffset + i * FinalMissionSetRecord.RecordSize, dataSegment);
            }

            // Find the string table boundaries: collect all non-empty slot string positions
            var allSlotPtrs = new List<int>();
            for (var i = 0; i < MissionSetCount; i++)
            {
                var recBase = MissionSetsOffset + i * FinalMissionSetRecord.RecordSize;
                for (var si = 0; si < FinalMissionSetRecord.TotalSlotStrings; si++)
                {
                    var ptr = BitConverter.ToUInt16(dataSegment, recBase + 0x2A + si * 2);
                    if (ptr > 0 && ptr < dataSegment.Length && dataSegment[ptr] != 0)
                    {
                        allSlotPtrs.Add(ptr);
                    }
                }
            }

            if (allSlotPtrs.Count > 0)
            {
                var tableStart = allSlotPtrs.Min();
                var tableEnd = tableStart;
                foreach (var ptr in allSlotPtrs)
                {
                    var e = ptr;
                    while (e < dataSegment.Length && dataSegment[e] != 0) e++;
                    e++;
                    if (e > tableEnd) tableEnd = e;
                }

                segment.PreStringTableData = DataSegmentHelper.Slice(dataSegment, 0, tableStart);
                segment.PostStringTableData = DataSegmentHelper.Slice(dataSegment, tableEnd, CopyrightOrgHeadOffset - tableEnd);

                // Compute trailing padding per record: null bytes between a record's last string
                // and the next record's first string
                for (var i = 0; i < MissionSetCount; i++)
                {
                    var ms = segment.MissionSets[i];
                    // Find end of this record's last non-empty string
                    var lastEnd = 0;
                    var recBase = MissionSetsOffset + i * FinalMissionSetRecord.RecordSize;
                    for (var si = 0; si < FinalMissionSetRecord.TotalSlotStrings; si++)
                    {
                        var ptr = BitConverter.ToUInt16(dataSegment, recBase + 0x2A + si * 2);
                        if (ptr > 0 && ptr < dataSegment.Length && dataSegment[ptr] != 0)
                        {
                            var e = (int)ptr;
                            while (e < dataSegment.Length && dataSegment[e] != 0) e++;
                            e++;
                            if (e > lastEnd) lastEnd = e;
                        }
                    }

                    // Find next record's first string start (or table end if last record)
                    var nextStart = tableEnd;
                    if (i + 1 < MissionSetCount)
                    {
                        var nextBase = MissionSetsOffset + (i + 1) * FinalMissionSetRecord.RecordSize;
                        for (var si = 0; si < FinalMissionSetRecord.TotalSlotStrings; si++)
                        {
                            var ptr = BitConverter.ToUInt16(dataSegment, nextBase + 0x2A + si * 2);
                            if (ptr > 0 && ptr < dataSegment.Length && dataSegment[ptr] != 0)
                            {
                                nextStart = ptr;
                                break;
                            }
                        }
                    }

                    ms.TrailingPadding = lastEnd > 0 ? nextStart - lastEnd : 0;
                }
            }
            else
            {
                segment.PreStringTableData = DataSegmentHelper.Slice(dataSegment, 0, CopyrightOrgHeadOffset);
                segment.PostStringTableData = Array.Empty<byte>();
            }

            segment.CopyrightOrgHeads = new CopyrightOrgHeadRecord[CopyrightOrgHeadCount];
            for (var i = 0; i < CopyrightOrgHeadCount; i++)
            {
                segment.CopyrightOrgHeads[i] = CopyrightOrgHeadRecord.FromBytes(dataSegment, CopyrightOrgHeadOffset + i * CopyrightOrgHeadRecordSize);
            }

            var missionSetsStart = CopyrightOrgHeadOffset + CopyrightOrgHeadSize;
            segment.Unknown1 = DataSegmentHelper.Slice(dataSegment, missionSetsStart, MissionSetsOffset - missionSetsStart);

            var missionSetsEnd = MissionSetsOffset + MissionSetCount * FinalMissionSetRecord.RecordSize;
            ParsePostMissionPreCrimeData(dataSegment, missionSetsEnd, CrimeTypesOffset, segment);

            // Crime type names: 13 null-terminated strings
            var crimeEnd = FindNthNullTerminator(dataSegment, CrimeTypesOffset, CrimeTypeCount);
            var (crimeNames, crimeSizes) = DataSegmentHelper.NullTerminatedStringsWithSizesFromBytes(dataSegment, CrimeTypesOffset, CrimeTypeCount);
            segment.CrimeTypeNames = crimeNames;
            segment.CrimeTypeNameByteSizes = crimeSizes;

            segment.Unknown2 = DataSegmentHelper.Slice(dataSegment, crimeEnd, OrgsOffset - crimeEnd);

            // Organisation names: 24 null-terminated strings
            var orgEnd = FindNthNullTerminator(dataSegment, OrgsOffset, OrgCount);
            var (orgNames, orgSizes) = DataSegmentHelper.NullTerminatedStringsWithSizesFromBytes(dataSegment, OrgsOffset, OrgCount);
            segment.OrganisationNames = orgNames;
            segment.OrganisationNameByteSizes = orgSizes;

            // Extract character names using the pointer table at CharNamePointersOffset.
            // This offset is fixed in the original binary. On re-parse of re-serialized data,
            // we recompute it from the serialization order.
            // For initial parse, use the constant. For re-parse, the pointer table follows
            // the PostCharNameData section. We handle both by trying the constant first.
            var charPtrTableOffset = FindCharNamePointerTableOffset(dataSegment, orgEnd);
            var charPtrs = DataSegmentHelper.BytesToUInt16Array(dataSegment, charPtrTableOffset, CharNamePointerCount);
            segment.CharacterNames = DataSegmentHelper.ExtractStringsFromPointers(charPtrs, dataSegment);

            var (charBlockStart, charBlockEnd) = DataSegmentHelper.FindStringBlockBounds(charPtrs, dataSegment);
            ParsePostOrgPreCharNameData(dataSegment, orgEnd, charBlockStart, segment);
            var postCharLen = charPtrTableOffset - charBlockEnd;
            segment.PostCharNameData = postCharLen > 0
                ? DataSegmentHelper.Slice(dataSegment, charBlockEnd, postCharLen)
                : Array.Empty<byte>();

            var charPtrsEnd = charPtrTableOffset + CharNamePointerCount * 2;
            segment.GameStateData = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, GameStateDataSize);
            var trailingStart = charPtrsEnd + GameStateDataSize;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, trailingStart, dataSegment.Length - trailingStart);

            return segment;
        }

        public byte[] ToBytes()
        {
            // Build the string table from all records' slot strings
            var tableBase = PreStringTableData.Length;
            var tableParts = new List<byte>();
            var perRecordStringOffsets = new List<int[]>();
            var perRecordNullPadStart = new List<int>();

            foreach (var ms in MissionSets)
            {
                var stringOffsets = new int[FinalMissionSetRecord.TotalSlotStrings];

                // Write strings sequentially. Each pointer points to:
                // - non-empty: the start of that string in the table
                // - empty: the current position (a null byte — either previous string's
                //   terminator or the padding area)
                // Write strings slot by slot. Within a slot, strings are back-to-back.
                // Between slots, there is 1 null byte gap (where empty second pointers point).
                var lastSlotWithContent = -1;
                for (var slot = 0; slot < FinalMissionSetRecord.CrimeSlotCount; slot++)
                {
                    var aIdx = slot * 2;
                    var bIdx = slot * 2 + 1;
                    var hasA = aIdx < ms.SlotStrings.Length && !string.IsNullOrEmpty(ms.SlotStrings[aIdx]);
                    var hasB = bIdx < ms.SlotStrings.Length && !string.IsNullOrEmpty(ms.SlotStrings[bIdx]);

                    if (!hasA && !hasB) continue;

                    // Inter-slot null byte gap: only when the previous slot had A but not B
                    // (the empty B pointer references the null terminator, creating a 1-byte gap)
                    if (lastSlotWithContent >= 0)
                    {
                        var prevBIdx = lastSlotWithContent * 2 + 1;
                        var prevHadB = prevBIdx < ms.SlotStrings.Length && !string.IsNullOrEmpty(ms.SlotStrings[prevBIdx]);
                        if (!prevHadB)
                        {
                            tableParts.Add(0);
                        }
                    }
                    lastSlotWithContent = slot;

                    if (hasA)
                    {
                        stringOffsets[aIdx] = tableBase + tableParts.Count;
                        tableParts.AddRange(Encoding.ASCII.GetBytes(ms.SlotStrings[aIdx]));
                        tableParts.Add(0); // null terminator
                    }

                    if (hasB)
                    {
                        stringOffsets[bIdx] = tableBase + tableParts.Count;
                        tableParts.AddRange(Encoding.ASCII.GetBytes(ms.SlotStrings[bIdx]));
                        tableParts.Add(0);
                    }
                    else if (hasA)
                    {
                        // Empty B pointer: point to A's null terminator
                        stringOffsets[bIdx] = tableBase + tableParts.Count - 1;
                    }
                }

                // Null padding area starts after all strings + final null terminator
                var padStart = tableBase + tableParts.Count;
                perRecordNullPadStart.Add(padStart);

                // Write trailing padding (provides null bytes for empty pointer fill + gap to next record)
                for (var p = 0; p < ms.TrailingPadding; p++)
                {
                    tableParts.Add(0);
                }

                // Resolve remaining empty pointer offsets: point into the trailing padding area
                var fillPos = padStart;
                for (var i = 0; i < FinalMissionSetRecord.TotalSlotStrings; i++)
                {
                    if (stringOffsets[i] == 0 && tableBase > 0)
                    {
                        stringOffsets[i] = fillPos;
                        fillPos++;
                    }
                }
                perRecordStringOffsets.Add(stringOffsets);
            }

            var stringTableBytes = tableParts.ToArray();

            // Build mission set record bytes with computed string pointers
            var missionSetBytes = new byte[MissionSets.Length * FinalMissionSetRecord.RecordSize];
            for (var i = 0; i < MissionSets.Length; i++)
            {
                Array.Copy(
                    MissionSets[i].ToBytes(perRecordStringOffsets[i], perRecordNullPadStart[i]),
                    0, missionSetBytes, i * FinalMissionSetRecord.RecordSize,
                    FinalMissionSetRecord.RecordSize);
            }

            var crimeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CrimeTypeNames, CrimeTypeNameByteSizes);
            var orgBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(OrganisationNames, OrganisationNameByteSizes);
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            // Serialize org appearance records
            var orgAppearanceBytes = new byte[CopyrightOrgHeadSize];
            for (var i = 0; i < CopyrightOrgHeads.Length; i++)
            {
                Array.Copy(CopyrightOrgHeads[i].ToBytes(), 0, orgAppearanceBytes, i * CopyrightOrgHeadRecordSize, CopyrightOrgHeadRecordSize);
            }

            // Build PostMissionPreCrimeData from sub-sections
            var postMissionBytes = BuildPostMissionPreCrimeData();

            // Build PostOrgPreCharNameData from sub-sections with recomputed pointer tables
            var postOrgBytes = BuildPostOrgPreCharNameData();

            // Compute character name pointer values
            var charNamesBaseOffset = PreStringTableData.Length + stringTableBytes.Length
                + PostStringTableData.Length + orgAppearanceBytes.Length
                + Unknown1.Length + missionSetBytes.Length + postMissionBytes.Length
                + crimeBytes.Length + Unknown2.Length + orgBytes.Length
                + postOrgBytes.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);

            var result = DataSegmentHelper.Concatenate(
                PreStringTableData,
                stringTableBytes,
                PostStringTableData,
                orgAppearanceBytes,
                Unknown1,
                missionSetBytes,
                postMissionBytes,
                crimeBytes,
                Unknown2,
                orgBytes,
                postOrgBytes,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                GameStateData,
                TrailingData
            );

            // Patch the RastPort config pointer now that we know the full layout
            var postMissionStart = PreStringTableData.Length + stringTableBytes.Length
                + PostStringTableData.Length + orgAppearanceBytes.Length
                + Unknown1.Length + missionSetBytes.Length;
            PatchRastPortConfigPointer(result, postMissionStart, postMissionBytes);

            // Patch all pointer tables in PostOrgPreCharNameData
            var postOrgStart = postMissionStart + postMissionBytes.Length
                + crimeBytes.Length + Unknown2.Length + orgBytes.Length;
            PatchPostOrgPointers(result, postOrgStart);

            return result;
        }

        public FinalDataSegment Clone()
        {
            return new FinalDataSegment
            {
                PreStringTableData = PreStringTableData.ToArray(),
                PostStringTableData = PostStringTableData.ToArray(),
                CopyrightOrgHeads = CopyrightOrgHeads.Select(o => o.Clone()).ToArray(),
                Unknown1 = Unknown1.ToArray(),
                MissionSets = MissionSets.Select(m => m.Clone()).ToArray(),
                PlotFileStrings = PlotFileStrings.Select(s => s).ToArray(),
                CharacterCreationStrings = CharacterCreationStrings.Select(s => s).ToArray(),
                SkillNames = SkillNames.Select(s => s).ToArray(),
                TrainingScreenStrings = TrainingScreenStrings.Select(s => s).ToArray(),
                TrainingScreenColumnXCoords = TrainingScreenColumnXCoords.ToArray(),
                TrainingScreenColorWords = TrainingScreenColorWords.ToArray(),
                TrainingScreenPaletteRemap = TrainingScreenPaletteRemap.ToArray(),
                TrainingScreenRemapTerminator = TrainingScreenRemapTerminator,
                CopyrightProtectionStrings = CopyrightProtectionStrings.Select(s => s).ToArray(),
                GameProgressStrings = GameProgressStrings.Select(s => s).ToArray(),
                RastPortDataOffset = RastPortDataOffset,
                RastPortPage = RastPortPage,
                RastPortOriginX = RastPortOriginX,
                RastPortOriginY = RastPortOriginY,
                RastPortExtentX = RastPortExtentX,
                RastPortExtentY = RastPortExtentY,
                RastPortFlag = RastPortFlag,
                RastPortMaxColor = RastPortMaxColor,
                RastPortBPP = RastPortBPP,
                RastPortReserved = RastPortReserved,
                PlotFileBuffer = PlotFileBuffer.ToArray(),
                ChronologyFormatStrings = ChronologyFormatStrings.Select(s => s).ToArray(),
                TimeTemplateBuffer = TimeTemplateBuffer,
                EfficiencyReportStrings = EfficiencyReportStrings.Select(s => s).ToArray(),
                CrimeTypeNames = CrimeTypeNames.Select(s => s).ToArray(),
                CrimeTypeNameByteSizes = CrimeTypeNameByteSizes.ToArray(),
                Unknown2 = Unknown2.ToArray(),
                OrganisationNames = OrganisationNames.Select(s => s).ToArray(),
                OrganisationNameByteSizes = OrganisationNameByteSizes.ToArray(),
                CareerReviewStrings = CareerReviewStrings.ToArray(),
                CareerReviewStringSizes = CareerReviewStringSizes.ToArray(),
                MissionEndStrings = MissionEndStrings.ToArray(),
                MissionEndStringSizes = MissionEndStringSizes.ToArray(),
                MissionEndSceneRecords = MissionEndSceneRecords.ToArray(),
                BriefingStrings = BriefingStrings.ToArray(),
                BriefingStringSizes = BriefingStringSizes.ToArray(),
                HallOfFameStrings = HallOfFameStrings.ToArray(),
                HallOfFameStringSizes = HallOfFameStringSizes.ToArray(),
                ClueRelationshipPhrases = ClueRelationshipPhrases.ToArray(),
                CluePhraseSizes = CluePhraseSizes.ToArray(),
                MonthAbbreviations = MonthAbbreviations.ToArray(),
                MonthSizes = MonthSizes.ToArray(),
                IntelHeaders = IntelHeaders.ToArray(),
                IntelHeaderSizes = IntelHeaderSizes.ToArray(),
                ClueCategoryData = ClueCategoryData.ToArray(),
                IntelMidPadding = IntelMidPadding.ToArray(),
                IntelReportTexts = IntelReportTexts.ToArray(),
                IntelReportTextSizes = IntelReportTextSizes.ToArray(),
                RankNames = RankNames.ToArray(),
                RankNameSizes = RankNameSizes.ToArray(),
                EvidenceTypeAbbreviations = EvidenceTypeAbbreviations.ToArray(),
                EvidenceTypeSizes = EvidenceTypeSizes.ToArray(),
                EvidenceItemNames = EvidenceItemNames.ToArray(),
                EvidenceItemSizes = EvidenceItemSizes.ToArray(),
                InvestigationMethods = InvestigationMethods.ToArray(),
                InvestigationMethodSizes = InvestigationMethodSizes.ToArray(),
                ClueSystemData = ClueSystemData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                GameStateData = GameStateData.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }

        #region PostMissionPreCrimeData parsing and serialization

        /// <summary>
        /// Parses the PostMissionPreCrimeData region (between mission sets and crime types)
        /// into typed sub-section fields. Uses string counting from known section boundaries.
        /// </summary>
        private static void ParsePostMissionPreCrimeData(byte[] data, int start, int end, FinalDataSegment segment)
        {
            var pos = start;

            // PlotFileStrings: 9 null-terminated strings
            segment.PlotFileStrings = ReadStrings(data, ref pos, PlotFileStringsCount);

            // CharacterCreationStrings: 2 null-terminated strings
            segment.CharacterCreationStrings = ReadStrings(data, ref pos, CharCreationStringsCount);

            // SkillNames: 5 null-terminated strings
            segment.SkillNames = ReadStrings(data, ref pos, SkillNameCount);

            // TrainingScreenStrings: read strings until we hit the column position header.
            // The column position header is a 10-byte binary prefix followed by "Average" in one
            // null-terminated string. We detect it by finding a string that starts with non-letter
            // bytes (the '$'-separated position data) and ends with "Average".
            var trainingStrings = new List<string>();
            while (pos < end)
            {
                var strEnd = pos;
                while (strEnd < end && data[strEnd] != 0) strEnd++;
                var raw = DataSegmentHelper.Slice(data, pos, strEnd - pos);

                // Detect the column position + Average combined string:
                // starts with non-letter ASCII (position bytes) and ends with "Average"
                if (raw.Length > 10 && Encoding.ASCII.GetString(raw, raw.Length - 7, 7) == "Average")
                {
                    // Split: extract x-coordinates from even indices, rest is "Average" label
                    var colPosLen = raw.Length - 7; // "Average" is 7 chars
                    var xCoords = new byte[colPosLen / 2];
                    for (var ci = 0; ci < xCoords.Length; ci++)
                        xCoords[ci] = raw[ci * 2];
                    segment.TrainingScreenColumnXCoords = xCoords;
                    trainingStrings.Add(Encoding.ASCII.GetString(raw, colPosLen, 7)); // "Average"
                    pos = strEnd + 1;
                    continue;
                }

                var s = Encoding.ASCII.GetString(data, pos, strEnd - pos);
                pos = strEnd + 1;
                trainingStrings.Add(s);

                if (s == "Awesome")
                    break;
            }
            segment.TrainingScreenStrings = trainingStrings.ToArray();

            // TrainingScreenColorWords: skip one null padding byte, then 4 uint16 words
            if (pos < end && data[pos] == 0) pos++; // skip padding null after Awesome
            segment.TrainingScreenColorWords = new ushort[TrainingColorWordCount];
            for (var i = 0; i < TrainingColorWordCount && pos + 1 < end; i++)
            {
                segment.TrainingScreenColorWords[i] = BitConverter.ToUInt16(data, pos);
                pos += 2;
            }

            // TrainingScreenPaletteRemap: 16 bytes
            segment.TrainingScreenPaletteRemap = DataSegmentHelper.Slice(data, pos, TrainingPaletteRemapSize);
            pos += TrainingPaletteRemapSize;

            // Trailing null terminator byte
            segment.TrainingScreenRemapTerminator = pos < end ? data[pos] : (byte)0;
            pos++;

            // CopyrightProtectionStrings: 2 null-terminated strings
            segment.CopyrightProtectionStrings = ReadStrings(data, ref pos, CopyrightProtStringsCount);

            // GameProgressStrings: read strings until we're within 22 bytes of the RastPort block.
            // The RastPort's DataOffset field (first 2 bytes) overlaps with the null terminator of the
            // last GameProgressStrings entry. We detect the RastPort by scanning for the
            // signature: 00 00 3F 01 C7 00 (OriginX=0, OriginY=0, ExtentX=319, ExtentY=199).
            var rastPortStart = FindRastPortInRegion(data, pos, end);
            var gameProgressStrings = new List<string>();
            // The RastPort DO field starts at rastPortStart, but its first 2 bytes overlap with
            // what precedes it (the last string's null terminator + the DO value).
            // Read strings up to rastPortStart + 2 (the DO field is part of the preceding region).
            var gameProgressEnd = rastPortStart;
            while (pos < gameProgressEnd)
            {
                var strEnd = pos;
                while (strEnd < gameProgressEnd && data[strEnd] != 0) strEnd++;
                var s = Encoding.ASCII.GetString(data, pos, strEnd - pos);
                pos = strEnd + 1;
                gameProgressStrings.Add(s);
            }
            segment.GameProgressStrings = gameProgressStrings.ToArray();

            // RastPort: 20 bytes (starts at rastPortStart, overlapping with the last string's null + DO)
            pos = rastPortStart;
            segment.RastPortDataOffset = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortPage = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortOriginX = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortOriginY = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortExtentX = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortExtentY = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortFlag = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortMaxColor = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortBPP = BitConverter.ToUInt16(data, pos); pos += 2;
            segment.RastPortReserved = BitConverter.ToUInt16(data, pos); pos += 2;

            // RastPort config pointer (2 bytes) — skip, will be recomputed
            pos += RastPortConfigPointerSize;

            // PlotFileBuffer: 9 bytes
            segment.PlotFileBuffer = DataSegmentHelper.Slice(data, pos, PlotFileBufferSize);
            pos += PlotFileBufferSize;

            // ChronologyFormatStrings: strings until " 10" (the last string before the time template).
            // Includes "You turned ", " " separator, and " 10" suffix used by FUN_2c94.
            var chronStrings = new List<string>();
            while (pos < end)
            {
                var strEnd = pos;
                while (strEnd < end && data[strEnd] != 0) strEnd++;
                var s = Encoding.ASCII.GetString(data, pos, strEnd - pos);
                chronStrings.Add(s);
                pos = strEnd + 1;
                if (s == " 10")
                    break;
            }
            segment.ChronologyFormatStrings = chronStrings.ToArray();

            // Null separator + TimeTemplateBuffer: "00:00 AM Jun 00"
            if (pos < end && data[pos] == 0) pos++; // skip null separator
            var tmplEnd = pos;
            while (tmplEnd < end && data[tmplEnd] != 0) tmplEnd++;
            segment.TimeTemplateBuffer = Encoding.ASCII.GetString(data, pos, tmplEnd - pos);
            pos = tmplEnd + 1;

            // EfficiencyReportStrings: all remaining strings to end of region
            var effStrings = new List<string>();
            while (pos < end)
            {
                var strEnd = pos;
                while (strEnd < end && data[strEnd] != 0) strEnd++;
                // Use Latin-1 to preserve 0x89 bytes faithfully
                var bytes = DataSegmentHelper.Slice(data, pos, strEnd - pos);
                var s = new string(Array.ConvertAll(bytes, b => (char)b));
                pos = strEnd + 1;
                effStrings.Add(s);
            }
            segment.EfficiencyReportStrings = effStrings.ToArray();
        }

        /// <summary>
        /// Serializes all PostMissionPreCrimeData sub-sections back into a contiguous byte array.
        /// Handles variable-length strings and recalculates the RastPort config pointer.
        /// </summary>
        private byte[] BuildPostMissionPreCrimeData()
        {
            var parts = new List<byte>();

            // PlotFileStrings
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToBytes(PlotFileStrings));

            // CharacterCreationStrings
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToBytes(CharacterCreationStrings));

            // SkillNames
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToBytes(SkillNames));

            // TrainingScreenStrings — recombine column positions with "Average" into one null-terminated string
            for (var i = 0; i < TrainingScreenStrings.Length; i++)
            {
                if (TrainingScreenStrings[i] == "Average" && TrainingScreenColumnXCoords.Length > 0)
                {
                    // Recombine: x-coords interleaved with '$' separators + "Average" + null
                    foreach (var x in TrainingScreenColumnXCoords)
                    {
                        parts.Add(x);
                        parts.Add(0x24); // '$'
                    }
                    parts.AddRange(Encoding.ASCII.GetBytes("Average"));
                    parts.Add(0);
                }
                else
                {
                    parts.AddRange(Encoding.ASCII.GetBytes(TrainingScreenStrings[i]));
                    parts.Add(0);
                }
            }

            // Padding null + TrainingScreenColorWords
            parts.Add(0);
            foreach (var w in TrainingScreenColorWords)
            {
                parts.Add((byte)(w & 0xFF));
                parts.Add((byte)((w >> 8) & 0xFF));
            }

            // TrainingScreenPaletteRemap + terminator
            parts.AddRange(TrainingScreenPaletteRemap);
            parts.Add(TrainingScreenRemapTerminator);

            // CopyrightProtectionStrings
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToBytes(CopyrightProtectionStrings));

            // GameProgressStrings — the last string's null terminator overlaps with RastPort DO
            var gpBytes = DataSegmentHelper.NullTerminatedStringsToBytes(GameProgressStrings);
            // Remove the final null terminator — it becomes the first byte of the RastPort DO field
            if (gpBytes.Length > 0)
            {
                parts.AddRange(DataSegmentHelper.Slice(gpBytes, 0, gpBytes.Length - 1));
            }

            // RastPort: 20 bytes. First 2 bytes (DO) include the null terminator from GameProgressStrings.
            // We write the full 20-byte block, with DO's low byte being 0x00 (the null terminator).
            WriteUInt16(parts, RastPortDataOffset);
            WriteUInt16(parts, RastPortPage);
            WriteUInt16(parts, RastPortOriginX);
            WriteUInt16(parts, RastPortOriginY);
            WriteUInt16(parts, RastPortExtentX);
            WriteUInt16(parts, RastPortExtentY);
            WriteUInt16(parts, RastPortFlag);
            WriteUInt16(parts, RastPortMaxColor);
            WriteUInt16(parts, RastPortBPP);
            WriteUInt16(parts, RastPortReserved);

            // RastPort config pointer: points to RastPort + 2 (the Page field).
            // This is a DS-relative offset. We need to compute the absolute DS offset of the
            // RastPort start within the full data segment. The PostMissionPreCrimeData region
            // starts at MissionSetsOffset + MissionSetCount * RecordSize in the original layout,
            // but with variable-length strings, we compute it relative to the start of this block.
            // The config pointer = DS offset of RastPort + 2.
            // At serialization time, the caller places this block at a known position.
            // We use a placeholder here and patch it in ToBytes().
            var rastPortConfigPtrPos = parts.Count;
            WriteUInt16(parts, 0x0000); // placeholder — patched below

            // PlotFileBuffer
            parts.AddRange(DataSegmentHelper.PadToSize(PlotFileBuffer, PlotFileBufferSize));

            // ChronologyFormatStrings
            parts.AddRange(DataSegmentHelper.NullTerminatedStringsToBytes(ChronologyFormatStrings));

            // Null separator + TimeTemplateBuffer
            parts.Add(0);
            parts.AddRange(Encoding.ASCII.GetBytes(TimeTemplateBuffer));
            parts.Add(0);

            // EfficiencyReportStrings — encode chars > 0x7F as raw bytes
            foreach (var s in EfficiencyReportStrings)
            {
                foreach (var c in s)
                    parts.Add((byte)c);
                parts.Add(0);
            }

            var result = parts.ToArray();

            // Patch the RastPort config pointer. The RastPort block starts at
            // (rastPortConfigPtrPos - RastPortConfigPointerSize - RastPortSize) within this byte array.
            // The config pointer = overall DS offset of (RastPort start + 2).
            // But we don't know the absolute DS offset here — we need the caller to provide it.
            // Instead, store the local offset and let ToBytes patch it.
            // Actually, we can compute it: PostMissionPreCrimeData starts at a known position
            // in the full data segment. We'll return the raw bytes and let ToBytes patch the pointer.

            return result;
        }

        /// <summary>
        /// Patches the RastPort config pointer in the serialized data segment.
        /// Called after the full data segment layout is known.
        /// </summary>
        private static void PatchRastPortConfigPointer(byte[] fullDataSegment, int postMissionStart, byte[] postMissionBytes)
        {
            // Find the RastPort signature within postMissionBytes
            var rpLocalOffset = FindRastPortInRegion(postMissionBytes, 0, postMissionBytes.Length);
            if (rpLocalOffset < 0) return;

            // The config pointer is at RastPort + 20 bytes
            var configPtrLocalOffset = rpLocalOffset + RastPortSize;
            if (configPtrLocalOffset + 1 >= postMissionBytes.Length) return;

            // Compute the DS-relative offset of the RastPort + 2 (config portion)
            var rastPortDsOffset = postMissionStart + rpLocalOffset + 2;
            fullDataSegment[postMissionStart + configPtrLocalOffset] = (byte)(rastPortDsOffset & 0xFF);
            fullDataSegment[postMissionStart + configPtrLocalOffset + 1] = (byte)((rastPortDsOffset >> 8) & 0xFF);
        }

        /// <summary>Finds the start of a RastPort block by scanning for the OriginX=0, OriginY=0, ExtentX=319, ExtentY=199 signature.</summary>
        private static int FindRastPortInRegion(byte[] data, int start, int end)
        {
            // Signature: 00 00 00 00 3F 01 C7 00 at RastPort + 4
            for (var i = start; i + RastPortSize <= end; i++)
            {
                if (i + 12 <= end
                    && data[i + 4] == 0x00 && data[i + 5] == 0x00   // OriginX = 0
                    && data[i + 6] == 0x00 && data[i + 7] == 0x00   // OriginY = 0
                    && data[i + 8] == 0x3F && data[i + 9] == 0x01   // ExtentX = 319
                    && data[i + 10] == 0xC7 && data[i + 11] == 0x00 // ExtentY = 199
                    && data[i + 14] == 0x0F && data[i + 15] == 0x00 // MaxColor = 15
                    && data[i + 16] == 0x04 && data[i + 17] == 0x00) // BPP = 4
                {
                    return i;
                }
            }
            return -1;
        }

        private static string[] ReadStrings(byte[] data, ref int pos, int count)
        {
            var result = new string[count];
            for (var i = 0; i < count; i++)
            {
                var strEnd = pos;
                while (strEnd < data.Length && data[strEnd] != 0) strEnd++;
                result[i] = Encoding.ASCII.GetString(data, pos, strEnd - pos);
                pos = strEnd + 1;
            }
            return result;
        }

        private static void WriteUInt16(List<byte> parts, ushort value)
        {
            parts.Add((byte)(value & 0xFF));
            parts.Add((byte)((value >> 8) & 0xFF));
        }

        #endregion

        #region PostOrgPreCharNameData parsing and serialization

        /// <summary>
        /// Parses the PostOrgPreCharNameData region (between org names and character names)
        /// into typed sub-section fields. Uses byte-boundary extraction with fixed sub-region sizes.
        /// </summary>
        private static void ParsePostOrgPreCharNameData(byte[] data, int start, int end, FinalDataSegment segment)
        {
            var pos = start;

            // TL group 1: Career review strings (203 bytes, 19 strings)
            var (careerStrs, careerSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, CareerReviewStringsByteSize);
            segment.CareerReviewStrings = careerStrs;
            segment.CareerReviewStringSizes = careerSzs;
            pos += CareerReviewStringsByteSize;

            // Crime/org pointer table (78 bytes = 39 uint16) — skip, recomputed
            pos += CrimeOrgPointerTableSize;

            // TL group 2: Mission end strings (395 bytes, 24 strings)
            var (meStrs, meSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, MissionEndStringsByteSize);
            segment.MissionEndStrings = meStrs;
            segment.MissionEndStringSizes = meSzs;
            pos += MissionEndStringsByteSize;

            // Scene pointers: 1 null + 4 uint16 = 9 bytes (skipped, recomputed at serialize)
            pos += ScenePointersByteSize;

            // Scene records: 21 x 5 uint16 = 210 bytes
            segment.MissionEndSceneRecords = new ushort[SceneRecordCount * SceneRecordWords];
            for (var i = 0; i < segment.MissionEndSceneRecords.Length; i++)
            {
                segment.MissionEndSceneRecords[i] = BitConverter.ToUInt16(data, pos);
                pos += 2;
            }

            // TM: Briefing + HoF strings.
            // Total TM = 1516 - CluePhrasesSize - MonthsSize - IntelHeadersSize = 790 bytes.
            // Within that, briefing = 638 bytes, HoF = 152 bytes (split at first "fame.dta").
            var tmStringSize = 1516 - CluePhrasesSize - MonthsSize - IntelHeadersSize; // 790
            var fameOffset = FindMarkerString(data, pos, pos + tmStringSize, "fame.dta");
            var briefingSize = fameOffset - pos;
            var hofSize = tmStringSize - briefingSize;

            var (briefingStrs, briefingSizes) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, briefingSize);
            segment.BriefingStrings = briefingStrs;
            segment.BriefingStringSizes = briefingSizes;
            pos += briefingSize;

            var (hofStrs, hofSizes) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, hofSize);
            segment.HallOfFameStrings = hofStrs;
            segment.HallOfFameStringSizes = hofSizes;
            pos += hofSize;

            // Clue relationship phrases (544 bytes)
            var (cluePhrases, clueSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, CluePhrasesSize);
            segment.ClueRelationshipPhrases = cluePhrases;
            segment.CluePhraseSizes = clueSzs;
            pos += CluePhrasesSize;

            // Month abbreviations (48 bytes)
            var (months, monthSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, MonthsSize);
            segment.MonthAbbreviations = months;
            segment.MonthSizes = monthSzs;
            pos += MonthsSize;

            // Intel headers (134 bytes)
            var (intelHdrs, intelHdrSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, IntelHeadersSize);
            segment.IntelHeaders = intelHdrs;
            segment.IntelHeaderSizes = intelHdrSzs;
            pos += IntelHeadersSize;

            // TN: Clue phrase pointer table (80 bytes) — skip, recomputed
            pos += CluePhrasePointerTableSize;

            // TN: Clue category bytes (40 bytes)
            segment.ClueCategoryData = DataSegmentHelper.Slice(data, pos, ClueCategoryDataSize);
            pos += ClueCategoryDataSize;

            // TO: Month pointer table (24 bytes) — skip, recomputed
            pos += MonthPointerTableSize;

            // TO: Padding (3 bytes)
            segment.IntelMidPadding = DataSegmentHelper.Slice(data, pos, IntelMidPaddingSize);
            pos += IntelMidPaddingSize;

            // TO: Intel report texts — from pos to rank names.
            // Rank names start with "Recruit\0". Scan for this marker.
            var rankStart = FindMarkerString(data, pos, end, "Recruit");
            var intelTextsSize = rankStart - pos;
            var (intelTexts, intelTextSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, intelTextsSize);
            segment.IntelReportTexts = intelTexts;
            segment.IntelReportTextSizes = intelTextSzs;
            pos = rankStart;

            // TO: Rank names — 8 strings, ending before evidence type abbreviations.
            // Evidence types start with "CAR\0".
            var evTypesStart = FindMarkerString(data, pos, end, "CAR");
            var rankSize = evTypesStart - pos;
            var (ranks, rankSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, rankSize);
            segment.RankNames = ranks;
            segment.RankNameSizes = rankSzs;
            pos = evTypesStart;

            // TO: Evidence type abbreviations — 8 strings, ending before evidence items.
            // Evidence items start with "Ford Escort".
            var evItemsStart = FindMarkerString(data, pos, end, "Ford Escort");
            var evTypesSize = evItemsStart - pos;
            var (evTypes, evTypeSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, evTypesSize);
            segment.EvidenceTypeAbbreviations = evTypes;
            segment.EvidenceTypeSizes = evTypeSzs;
            pos = evItemsStart;

            // TO: Evidence item names — strings until evidence/rank pointer table.
            // The pointer table is 160 bytes of uint16 values that point back into ranks/types/items.
            // Find it by scanning for the first pointer value that matches rankStart's DS offset.
            // Simpler: evidence items end 1 byte before the pointer table. The pointer table
            // starts immediately after the last evidence item null terminator + 1 padding null.
            // From investigation: evidence items end at pos + N, then 1 null, then 160-byte pointer table.
            // We can find the pointer table by looking for a sequence of uint16 values that all
            // resolve to valid string starts. Or: just scan for "Clandestine Photo" to find
            // investigation methods, then back-calculate.
            var invMethodsStart = FindMarkerString(data, pos, end, "Clandestine Photo");
            var evPtrTableStart = invMethodsStart - EvidenceRankPointerTableSize;
            var evItemsSize = evPtrTableStart - pos;
            var (evItems, evItemSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, evItemsSize);
            segment.EvidenceItemNames = evItems;
            segment.EvidenceItemSizes = evItemSzs;
            pos = invMethodsStart; // skip pointer table (recomputed)

            // TO: Investigation methods — 8 strings ending before clue system data.
            // Clue system starts with bytes containing 0x87 control code ("Source:").
            // From investigation: inv methods are 134 bytes (same as TAC).
            // Find the end by counting 8 strings.
            var invPos = pos;
            for (var i = 0; i < 8; i++)
            {
                while (invPos < end && data[invPos] != 0) invPos++;
                invPos++; // skip null
            }
            var invSize = invPos - pos;
            var (invMethods, invMethodSzs) = DataSegmentHelper.AllNullTerminatedStringsWithSizesFromBytes(
                data, pos, invSize);
            segment.InvestigationMethods = invMethods;
            segment.InvestigationMethodSizes = invMethodSzs;
            pos = invPos;

            // TO: Clue system data — everything remaining
            segment.ClueSystemData = DataSegmentHelper.Slice(data, pos, end - pos);
        }

        /// <summary>Finds a marker string in the data segment by scanning for its ASCII bytes.</summary>
        private static int FindMarkerString(byte[] data, int start, int end, string marker)
        {
            var markerBytes = Encoding.ASCII.GetBytes(marker);
            for (var i = start; i <= end - markerBytes.Length; i++)
            {
                var match = true;
                for (var j = 0; j < markerBytes.Length; j++)
                {
                    if (data[i + j] != markerBytes[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return end; // fallback
        }

        /// <summary>
        /// Builds the PostOrgPreCharNameData byte array from sub-section fields,
        /// recomputing all pointer tables from string positions.
        /// </summary>
        private byte[] BuildPostOrgPreCharNameData()
        {
            // Serialize all string sections with fixed sizes for roundtrip fidelity
            var careerReviewBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CareerReviewStrings, CareerReviewStringSizes);
            var missionEndBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(MissionEndStrings, MissionEndStringSizes);
            var briefingBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(BriefingStrings, BriefingStringSizes);
            var hofBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(HallOfFameStrings, HallOfFameStringSizes);
            var clueBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(ClueRelationshipPhrases, CluePhraseSizes);
            var monthBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(MonthAbbreviations, MonthSizes);
            var intelHdrBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelHeaders, IntelHeaderSizes);
            var intelTxtBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelReportTexts, IntelReportTextSizes);
            var rankBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(RankNames, RankNameSizes);
            var evTypeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceTypeAbbreviations, EvidenceTypeSizes);
            var evItemBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceItemNames, EvidenceItemSizes);
            var invMethodBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(InvestigationMethods, InvestigationMethodSizes);

            // Crime/org pointer table (78 bytes) — placeholder, patched after assembly
            var crimeOrgPtrPlaceholder = new byte[CrimeOrgPointerTableSize];

            // Scene pointer data: 1 null separator + 4 uint16 pointers — placeholder, patched
            var scenePointerData = new byte[ScenePointersByteSize];

            // Scene record bytes
            var sceneRecordData = new byte[SceneRecordsByteSize];
            for (var i = 0; i < MissionEndSceneRecords.Length; i++)
            {
                sceneRecordData[i * 2] = (byte)(MissionEndSceneRecords[i] & 0xFF);
                sceneRecordData[i * 2 + 1] = (byte)((MissionEndSceneRecords[i] >> 8) & 0xFF);
            }

            // Other pointer table placeholders — patched after assembly
            var cluePtrPlaceholder = new byte[CluePhrasePointerTableSize];
            var monthPtrPlaceholder = new byte[MonthPointerTableSize];
            var evRankPtrPlaceholder = new byte[EvidenceRankPointerTableSize];

            var result = DataSegmentHelper.Concatenate(
                careerReviewBytes,
                crimeOrgPtrPlaceholder,
                missionEndBytes,
                scenePointerData,
                sceneRecordData,
                briefingBytes,
                hofBytes,
                clueBytes,
                monthBytes,
                intelHdrBytes,
                cluePtrPlaceholder,
                ClueCategoryData,
                monthPtrPlaceholder,
                IntelMidPadding,
                intelTxtBytes,
                rankBytes,
                evTypeBytes,
                evItemBytes,
                evRankPtrPlaceholder,
                invMethodBytes,
                ClueSystemData
            );

            return result;
        }

        /// <summary>
        /// Patches all pointer tables in the PostOrgPreCharNameData block after the full
        /// data segment layout is known. Called from ToBytes() after assembly.
        /// </summary>
        private void PatchPostOrgPointers(byte[] fullDataSegment, int postOrgStart)
        {
            var careerReviewBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CareerReviewStrings, CareerReviewStringSizes);
            var missionEndBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(MissionEndStrings, MissionEndStringSizes);
            var briefingBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(BriefingStrings, BriefingStringSizes);
            var hofBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(HallOfFameStrings, HallOfFameStringSizes);
            var clueBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(ClueRelationshipPhrases, CluePhraseSizes);
            var monthBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(MonthAbbreviations, MonthSizes);
            var intelHdrBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelHeaders, IntelHeaderSizes);
            var intelTxtBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(IntelReportTexts, IntelReportTextSizes);
            var rankBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(RankNames, RankNameSizes);
            var evTypeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceTypeAbbreviations, EvidenceTypeSizes);
            var evItemBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(EvidenceItemNames, EvidenceItemSizes);

            // Cumulative offsets within postOrgBlock
            var careerReviewStart = 0;
            var crimeOrgPtrStart = careerReviewStart + careerReviewBytes.Length;
            var missionEndStart = crimeOrgPtrStart + CrimeOrgPointerTableSize;
            var scenePtrStart = missionEndStart + missionEndBytes.Length;
            var sceneRecStart = scenePtrStart + ScenePointersByteSize;
            var briefStart = sceneRecStart + SceneRecordsByteSize;
            var hofStart = briefStart + briefingBytes.Length;
            var clueStart = hofStart + hofBytes.Length;
            var monthStart = clueStart + clueBytes.Length;
            var intelHdrStart = monthStart + monthBytes.Length;
            var cluePtrStart = intelHdrStart + intelHdrBytes.Length;
            var catStart = cluePtrStart + CluePhrasePointerTableSize;
            var monthPtrStart = catStart + ClueCategoryDataSize;
            var paddingStart = monthPtrStart + MonthPointerTableSize;
            var intelTxtStart = paddingStart + IntelMidPaddingSize;
            var rankStart = intelTxtStart + intelTxtBytes.Length;
            var evTypeStart = rankStart + rankBytes.Length;
            var evItemStart = evTypeStart + evTypeBytes.Length;
            var evPtrStart = evItemStart + evItemBytes.Length;

            // 1. Crime/org pointer table: 13 crime type pointers + 26 org name pointers
            // These point OUTSIDE postOrg, into CrimeTypeNames and OrganisationNames before this block.
            var crimeNamesBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CrimeTypeNames, CrimeTypeNameByteSizes);
            var orgNamesBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(OrganisationNames, OrganisationNameByteSizes);
            var orgStart = postOrgStart - orgNamesBytes.Length;
            var unknown2Start = orgStart - Unknown2.Length;
            var crimeStart = unknown2Start - crimeNamesBytes.Length;

            var crimePtrs = DataSegmentHelper.ComputeStringPointers(CrimeTypeNames, crimeStart);
            var orgPtrs = DataSegmentHelper.ComputeStringPointers(OrganisationNames, orgStart);
            for (var i = 0; i < CrimePointerCount && i < crimePtrs.Length; i++)
            {
                fullDataSegment[postOrgStart + crimeOrgPtrStart + i * 2] = (byte)(crimePtrs[i] & 0xFF);
                fullDataSegment[postOrgStart + crimeOrgPtrStart + i * 2 + 1] = (byte)((crimePtrs[i] >> 8) & 0xFF);
            }
            for (var i = 0; i < OrgPointerCount && i < orgPtrs.Length; i++)
            {
                var offset = (CrimePointerCount + i) * 2;
                fullDataSegment[postOrgStart + crimeOrgPtrStart + offset] = (byte)(orgPtrs[i] & 0xFF);
                fullDataSegment[postOrgStart + crimeOrgPtrStart + offset + 1] = (byte)((orgPtrs[i] >> 8) & 0xFF);
            }

            // 2. Scene filename pointers (4 uint16 at scenePtrStart + 1)
            var sceneIndices = FindSceneFilenameIndices(MissionEndStrings);
            var mePtrs = DataSegmentHelper.ComputeStringPointers(MissionEndStrings, postOrgStart + missionEndStart);
            for (var i = 0; i < SceneFilenameCount; i++)
            {
                var idx = sceneIndices[i];
                if (idx < 0) continue;
                var ptr = mePtrs[idx];
                fullDataSegment[postOrgStart + scenePtrStart + 1 + i * 2] = (byte)(ptr & 0xFF);
                fullDataSegment[postOrgStart + scenePtrStart + 1 + i * 2 + 1] = (byte)((ptr >> 8) & 0xFF);
            }

            // 3. Clue phrase pointers (40 uint16 at cluePtrStart)
            var cluePtrs = DataSegmentHelper.ComputeStringPointers(ClueRelationshipPhrases, postOrgStart + clueStart);
            for (var i = 0; i < cluePtrs.Length; i++)
            {
                fullDataSegment[postOrgStart + cluePtrStart + i * 2] = (byte)(cluePtrs[i] & 0xFF);
                fullDataSegment[postOrgStart + cluePtrStart + i * 2 + 1] = (byte)((cluePtrs[i] >> 8) & 0xFF);
            }

            // 4. Month pointers (12 uint16 at monthPtrStart)
            var monthPtrs = DataSegmentHelper.ComputeStringPointers(MonthAbbreviations, postOrgStart + monthStart);
            for (var i = 0; i < monthPtrs.Length; i++)
            {
                fullDataSegment[postOrgStart + monthPtrStart + i * 2] = (byte)(monthPtrs[i] & 0xFF);
                fullDataSegment[postOrgStart + monthPtrStart + i * 2 + 1] = (byte)((monthPtrs[i] >> 8) & 0xFF);
            }

            // 5. Evidence/rank pointer table (pointers to ranks + types + items)
            var allEvStrings = new string[RankNames.Length + EvidenceTypeAbbreviations.Length + EvidenceItemNames.Length];
            Array.Copy(RankNames, 0, allEvStrings, 0, RankNames.Length);
            Array.Copy(EvidenceTypeAbbreviations, 0, allEvStrings, RankNames.Length, EvidenceTypeAbbreviations.Length);
            Array.Copy(EvidenceItemNames, 0, allEvStrings, RankNames.Length + EvidenceTypeAbbreviations.Length, EvidenceItemNames.Length);
            var evPtrs = DataSegmentHelper.ComputeStringPointers(allEvStrings, postOrgStart + rankStart);
            for (var i = 0; i < evPtrs.Length && i * 2 + 1 < EvidenceRankPointerTableSize; i++)
            {
                fullDataSegment[postOrgStart + evPtrStart + i * 2] = (byte)(evPtrs[i] & 0xFF);
                fullDataSegment[postOrgStart + evPtrStart + i * 2 + 1] = (byte)((evPtrs[i] >> 8) & 0xFF);
            }
        }

        /// <summary>Finds indices of the 4 scene filename strings ("lau","off","bch","cas") in MissionEndStrings.</summary>
        private static int[] FindSceneFilenameIndices(string[] strings)
        {
            var targets = new[] { "lau", "off", "bch", "cas" };
            var indices = new int[targets.Length];
            for (var t = 0; t < targets.Length; t++)
            {
                indices[t] = -1;
                for (var i = 0; i < strings.Length; i++)
                {
                    if (strings[i] == targets[t])
                    {
                        indices[t] = i;
                        break;
                    }
                }
            }
            return indices;
        }

        #endregion

        /// <summary>
        /// Finds the character name pointer table offset. Uses the known constant for original
        /// binary data, with a validation check. For re-serialized data where the constant
        /// doesn't match, scans backwards from the end of the data segment.
        /// </summary>
        private static int FindCharNamePointerTableOffset(byte[] dataSegment, int searchStart)
        {
            // Try the known constant first
            var knownOffset = 0x4526; // CharNamePointersOffset from original binary
            if (knownOffset + CharNamePointerCount * 2 <= dataSegment.Length)
            {
                // Validate: first pointer should resolve to a printable ASCII string
                var firstPtr = BitConverter.ToUInt16(dataSegment, knownOffset);
                if (firstPtr > 0 && firstPtr < dataSegment.Length && dataSegment[firstPtr] >= 0x20 && dataSegment[firstPtr] < 0x7F)
                {
                    return knownOffset;
                }
            }

            // Fallback: scan backwards from end of data segment looking for the pointer table.
            // The table is followed by TrailingData. The last pointer should resolve to the
            // last character name. Scan for a 384-byte block where most values resolve to strings.
            var tableSize = CharNamePointerCount * 2;
            for (var offset = dataSegment.Length - tableSize; offset >= searchStart; offset -= 2)
            {
                var validCount = 0;
                for (var i = 0; i < Math.Min(10, CharNamePointerCount); i++)
                {
                    var ptr = BitConverter.ToUInt16(dataSegment, offset + i * 2);
                    if (ptr > 0 && ptr < dataSegment.Length && dataSegment[ptr] >= 0x20 && dataSegment[ptr] < 0x7F)
                    {
                        validCount++;
                    }
                }
                if (validCount >= 8) return offset; // Most of first 10 pointers valid
            }

            throw new InvalidOperationException("Could not find character name pointer table in FINAL data segment");
        }

        private static int FindNthNullTerminator(byte[] data, int startOffset, int count)
        {
            var pos = startOffset;
            for (var i = 0; i < count; i++)
            {
                while (pos < data.Length && data[pos] != 0) pos++;
                pos++; // skip the null terminator
            }
            return pos;
        }
    }
}
