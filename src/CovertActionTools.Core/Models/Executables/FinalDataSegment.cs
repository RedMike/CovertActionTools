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
    /// FUN_1100_11b7 packs these fields into a portrait layer descriptor. FUN_1100_7afe
    /// renders the portrait by drawing 5 sprite layers from FACES (male) or FACESF (female)
    /// images, plus clothing drawn below the face. Skin and hair colours are applied via
    /// VGA palette colour replacement at draw time.
    /// </summary>
    public class OrgAppearanceRecord
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

        public OrgAppearanceRecord Clone()
        {
            return new OrgAppearanceRecord
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

        public static OrgAppearanceRecord FromBytes(byte[] data, int offset)
        {
            return new OrgAppearanceRecord
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
        private const int OrgAppearanceOffset = 0x1CFC;    // 0x012A7C - 0x10D80
        private const int OrgAppearanceCount = 26;
        private const int OrgAppearanceRecordSize = 16;
        private const int OrgAppearanceSize = OrgAppearanceCount * OrgAppearanceRecordSize; // 416
        private const int MissionSetsOffset = 0x1E9E;      // 0x012C1E - 0x10D80
        private const int MissionSetCount = 16;
        private const int CrimeTypesOffset = 0x2AFC;        // 0x01387C - 0x10D80
        private const int CrimeTypeCount = 13;
        private const int OrgsOffset = 0x2B88;              // 0x013908 - 0x10D80
        private const int OrgCount = 24;
        private const int CharNamePointerCount = 192;
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
        /// FUN_1100_11b7 packs these fields into a portrait layer descriptor used by
        /// FUN_1100_7afe to render layered character portraits from FACES/FACESF sprites.
        /// </summary>
        public OrgAppearanceRecord[] OrgAppearances { get; set; } = Array.Empty<OrgAppearanceRecord>();

        /// <summary>2-byte gap between params and mission set records.</summary>
        public byte[] Unknown1 { get; set; } = Array.Empty<byte>();

        /// <summary>16 mission set records (74 bytes each): name, crime IDs, flags, string pointers.</summary>
        public FinalMissionSetRecord[] MissionSets { get; set; } = Array.Empty<FinalMissionSetRecord>();

        /// <summary>Data between mission sets and crime types: plot/briefing strings, skill names, case text.</summary>
        public byte[] PostMissionPreCrimeData { get; set; } = Array.Empty<byte>();

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

        /// <summary>Data between org names and character names: career text, briefing, clue phrases, item tables.</summary>
        public byte[] PostOrgPreCharNameData { get; set; } = Array.Empty<byte>();

        /// <summary>192 character names (4 ethnic groups x female first / male first / male surname, 16 each).</summary>
        public string[] CharacterNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between character names and trailing data (gap before pointer table position).</summary>
        public byte[] PostCharNameData { get; set; } = Array.Empty<byte>();

        // CharacterNamePointers are computed at serialization time from CharacterNames positions.

        /// <summary>Everything after character name pointers: game state, file management, runtime, BSS.</summary>
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
                segment.PostStringTableData = DataSegmentHelper.Slice(dataSegment, tableEnd, OrgAppearanceOffset - tableEnd);

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
                segment.PreStringTableData = DataSegmentHelper.Slice(dataSegment, 0, OrgAppearanceOffset);
                segment.PostStringTableData = Array.Empty<byte>();
            }

            segment.OrgAppearances = new OrgAppearanceRecord[OrgAppearanceCount];
            for (var i = 0; i < OrgAppearanceCount; i++)
            {
                segment.OrgAppearances[i] = OrgAppearanceRecord.FromBytes(dataSegment, OrgAppearanceOffset + i * OrgAppearanceRecordSize);
            }

            var missionSetsStart = OrgAppearanceOffset + OrgAppearanceSize;
            segment.Unknown1 = DataSegmentHelper.Slice(dataSegment, missionSetsStart, MissionSetsOffset - missionSetsStart);

            var missionSetsEnd = MissionSetsOffset + MissionSetCount * FinalMissionSetRecord.RecordSize;
            segment.PostMissionPreCrimeData = DataSegmentHelper.Slice(dataSegment, missionSetsEnd, CrimeTypesOffset - missionSetsEnd);

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
            segment.PostOrgPreCharNameData = DataSegmentHelper.Slice(dataSegment, orgEnd, charBlockStart - orgEnd);
            var postCharLen = charPtrTableOffset - charBlockEnd;
            segment.PostCharNameData = postCharLen > 0
                ? DataSegmentHelper.Slice(dataSegment, charBlockEnd, postCharLen)
                : Array.Empty<byte>();

            var charPtrsEnd = charPtrTableOffset + CharNamePointerCount * 2;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, dataSegment.Length - charPtrsEnd);

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
            var orgAppearanceBytes = new byte[OrgAppearanceSize];
            for (var i = 0; i < OrgAppearances.Length; i++)
            {
                Array.Copy(OrgAppearances[i].ToBytes(), 0, orgAppearanceBytes, i * OrgAppearanceRecordSize, OrgAppearanceRecordSize);
            }

            // Compute character name pointer values
            var charNamesBaseOffset = PreStringTableData.Length + stringTableBytes.Length
                + PostStringTableData.Length + orgAppearanceBytes.Length
                + Unknown1.Length + missionSetBytes.Length + PostMissionPreCrimeData.Length
                + crimeBytes.Length + Unknown2.Length + orgBytes.Length
                + PostOrgPreCharNameData.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);

            return DataSegmentHelper.Concatenate(
                PreStringTableData,
                stringTableBytes,
                PostStringTableData,
                orgAppearanceBytes,
                Unknown1,
                missionSetBytes,
                PostMissionPreCrimeData,
                crimeBytes,
                Unknown2,
                orgBytes,
                PostOrgPreCharNameData,
                charNamesBytes,
                PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(charNamePointers),
                TrailingData
            );
        }

        public FinalDataSegment Clone()
        {
            return new FinalDataSegment
            {
                PreStringTableData = PreStringTableData.ToArray(),
                PostStringTableData = PostStringTableData.ToArray(),
                OrgAppearances = OrgAppearances.Select(o => o.Clone()).ToArray(),
                Unknown1 = Unknown1.ToArray(),
                MissionSets = MissionSets.Select(m => m.Clone()).ToArray(),
                PostMissionPreCrimeData = PostMissionPreCrimeData.ToArray(),
                CrimeTypeNames = CrimeTypeNames.Select(s => s).ToArray(),
                CrimeTypeNameByteSizes = CrimeTypeNameByteSizes.ToArray(),
                Unknown2 = Unknown2.ToArray(),
                OrganisationNames = OrganisationNames.Select(s => s).ToArray(),
                OrganisationNameByteSizes = OrganisationNameByteSizes.ToArray(),
                PostOrgPreCharNameData = PostOrgPreCharNameData.ToArray(),
                CharacterNames = CharacterNames.Select(s => s).ToArray(),
                PostCharNameData = PostCharNameData.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
        }

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
