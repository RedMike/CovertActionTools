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
        public const int CrimeSlotCount = 3;
        public const int UnusedSlotCount = 8;
        public const int StringPointerCount = 16;
        public const int CrimeSlotTotal = 7;

        /// <summary>Mission set name, null-padded to 25 bytes.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Unknown bitfield at offset 0x19. Possibly encodes world area eligibility.</summary>
        public byte Unknown1 { get; set; }

        /// <summary>First crime type ID (index into crime type names).</summary>
        public ushort Crime1Id { get; set; }

        /// <summary>Second crime type ID.</summary>
        public ushort Crime2Id { get; set; }

        /// <summary>Third crime type ID.</summary>
        public ushort Crime3Id { get; set; }

        /// <summary>Unused crime slots (8 bytes, always 0xFF). Reserved for additional crimes.</summary>
        public byte[] UnusedCrimeSlots { get; set; } = Array.Empty<byte>();

        /// <summary>Flag word: 0x0001 for records 0-8, 0xFFFF for records 9-14. Purpose unknown.</summary>
        public ushort FlagWord { get; set; }

        // TODO: Each slot currently holds one string (the victim/target name), but the game
        // likely uses multiple strings per crime (victim, location, item, etc.). Investigate
        // via Ghidra disassembly how FUN_1100_0906 and related functions use the full pointer
        // pair range to determine if additional strings are referenced per slot.
        /// <summary>
        /// 7 crime slot strings (victim/item names). The game accesses these as 7 (start, end)
        /// pointer pairs at record offsets +0x2A through +0x44, indexed by crime slot (0-6).
        /// Empty slots contain empty strings. The remaining 2 words at +0x46/+0x48 are unused.
        /// </summary>
        public string[] SlotStrings { get; set; } = Array.Empty<string>();

        // SlotString pointer pairs are computed at serialization time from string positions.

        /// <summary>Number of null padding bytes after this record's last string before the next record's strings.</summary>
        public int TrailingPadding { get; set; }

        public FinalMissionSetRecord Clone()
        {
            return new FinalMissionSetRecord
            {
                Name = Name,
                Unknown1 = Unknown1,
                Crime1Id = Crime1Id,
                Crime2Id = Crime2Id,
                Crime3Id = Crime3Id,
                UnusedCrimeSlots = UnusedCrimeSlots.ToArray(),
                FlagWord = FlagWord,
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

            // Extract 7 slot strings from (start, end) pointer pairs
            var slotStrings = new string[CrimeSlotTotal];
            for (var slot = 0; slot < CrimeSlotTotal; slot++)
            {
                var start = BitConverter.ToUInt16(data, offset + 0x2A + slot * 4);
                var end = BitConverter.ToUInt16(data, offset + 0x2A + slot * 4 + 2);
                if (start > 0 && start < dataSegment.Length && end > start && end <= dataSegment.Length)
                {
                    var strEnd = start;
                    while (strEnd < dataSegment.Length && strEnd < end && dataSegment[strEnd] != 0) strEnd++;
                    slotStrings[slot] = strEnd > start
                        ? Encoding.ASCII.GetString(dataSegment, start, strEnd - start)
                        : string.Empty;
                }
                else
                {
                    slotStrings[slot] = string.Empty;
                }
            }

            return new FinalMissionSetRecord
            {
                Name = name,
                Unknown1 = data[offset + 0x19],
                Crime1Id = BitConverter.ToUInt16(data, offset + 0x1A),
                Crime2Id = BitConverter.ToUInt16(data, offset + 0x1C),
                Crime3Id = BitConverter.ToUInt16(data, offset + 0x1E),
                UnusedCrimeSlots = DataSegmentHelper.Slice(data, offset + 0x20, UnusedSlotCount),
                FlagWord = BitConverter.ToUInt16(data, offset + 0x28),
                SlotStrings = slotStrings
            };
        }

        /// <summary>
        /// Serializes the record to 74 bytes. slotStringOffsets must contain the DS-relative
        /// byte offset of each of the 7 slot strings (or the offset of the null padding area
        /// for empty slots). nullPadStart is the offset where null padding begins after the
        /// last real string.
        /// </summary>
        public byte[] ToBytes(int[] slotStringOffsets, int nullPadStart)
        {
            var result = new byte[RecordSize];
            var nameBytes = Encoding.ASCII.GetBytes(Name);
            Array.Copy(nameBytes, 0, result, 0, Math.Min(nameBytes.Length, NameLength));
            result[0x19] = Unknown1;
            result[0x1A] = (byte)(Crime1Id & 0xFF); result[0x1B] = (byte)((Crime1Id >> 8) & 0xFF);
            result[0x1C] = (byte)(Crime2Id & 0xFF); result[0x1D] = (byte)((Crime2Id >> 8) & 0xFF);
            result[0x1E] = (byte)(Crime3Id & 0xFF); result[0x1F] = (byte)((Crime3Id >> 8) & 0xFF);
            Array.Copy(UnusedCrimeSlots, 0, result, 0x20, Math.Min(UnusedCrimeSlots.Length, UnusedSlotCount));
            result[0x28] = (byte)(FlagWord & 0xFF); result[0x29] = (byte)((FlagWord >> 8) & 0xFF);

            // Build 16 pointer words: 7 (start, end) pairs + 2 trailing
            var words = new ushort[StringPointerCount];
            var fillPos = nullPadStart;
            for (var slot = 0; slot < CrimeSlotTotal; slot++)
            {
                var strOffset = slotStringOffsets[slot];
                words[slot * 2] = (ushort)strOffset;
                if (!string.IsNullOrEmpty(SlotStrings[slot]))
                {
                    var endOffset = strOffset + Encoding.ASCII.GetByteCount(SlotStrings[slot]) + 1;
                    words[slot * 2 + 1] = (ushort)endOffset;
                }
                else
                {
                    // Empty slot: (start, start+1) pointing into null padding
                    words[slot * 2] = (ushort)fillPos;
                    words[slot * 2 + 1] = (ushort)(fillPos + 1);
                    fillPos += 2;
                }
            }
            // Last 2 words: continue fill
            words[14] = (ushort)fillPos;
            words[15] = (ushort)(fillPos + 1);

            var ptrBytes = DataSegmentHelper.UInt16ArrayToBytes(words);
            Array.Copy(ptrBytes, 0, result, 0x2A, Math.Min(ptrBytes.Length, StringPointerCount * 2));
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
        private const int MissionParamsOffset = 0x1CFC;    // 0x012A7C - 0x10D80
        private const int MissionParamCount = 16;
        private const int MissionParamRecordSize = 26;
        private const int MissionParamsSize = MissionParamCount * MissionParamRecordSize; // 416
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

        /// <summary>16 x 26-byte mission set parameter records (values 0-8, partially understood).</summary>
        public byte[] MissionSetParameters { get; set; } = Array.Empty<byte>();

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
                for (var slot = 0; slot < FinalMissionSetRecord.CrimeSlotTotal; slot++)
                {
                    var start = BitConverter.ToUInt16(dataSegment, recBase + 0x2A + slot * 4);
                    if (start > 0 && start < dataSegment.Length && dataSegment[start] != 0)
                    {
                        allSlotPtrs.Add(start);
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
                segment.PostStringTableData = DataSegmentHelper.Slice(dataSegment, tableEnd, MissionParamsOffset - tableEnd);

                // Compute trailing padding per record: null bytes between a record's last string
                // and the next record's first string
                for (var i = 0; i < MissionSetCount; i++)
                {
                    var ms = segment.MissionSets[i];
                    // Find end of this record's last non-empty string
                    var lastEnd = 0;
                    var recBase = MissionSetsOffset + i * FinalMissionSetRecord.RecordSize;
                    for (var slot = 0; slot < FinalMissionSetRecord.CrimeSlotTotal; slot++)
                    {
                        var start = BitConverter.ToUInt16(dataSegment, recBase + 0x2A + slot * 4);
                        if (start > 0 && start < dataSegment.Length && dataSegment[start] != 0)
                        {
                            var e = start;
                            while (e < dataSegment.Length && dataSegment[e] != 0) e++;
                            e++; // past null
                            if (e > lastEnd) lastEnd = e;
                        }
                    }

                    // Find next record's first string start (or table end if last record)
                    var nextStart = tableEnd;
                    if (i + 1 < MissionSetCount)
                    {
                        var nextBase = MissionSetsOffset + (i + 1) * FinalMissionSetRecord.RecordSize;
                        for (var slot = 0; slot < FinalMissionSetRecord.CrimeSlotTotal; slot++)
                        {
                            var start = BitConverter.ToUInt16(dataSegment, nextBase + 0x2A + slot * 4);
                            if (start > 0 && start < dataSegment.Length && dataSegment[start] != 0)
                            {
                                nextStart = start;
                                break;
                            }
                        }
                    }

                    ms.TrailingPadding = lastEnd > 0 ? nextStart - lastEnd : 0;
                }
            }
            else
            {
                segment.PreStringTableData = DataSegmentHelper.Slice(dataSegment, 0, MissionParamsOffset);
                segment.PostStringTableData = Array.Empty<byte>();
            }

            segment.MissionSetParameters = DataSegmentHelper.Slice(dataSegment, MissionParamsOffset, MissionParamsSize);

            var missionSetsStart = MissionParamsOffset + MissionParamsSize;
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
            var perRecordSlotOffsets = new List<int[]>();
            var perRecordNullPadStart = new List<int>();

            foreach (var ms in MissionSets)
            {
                var slotOffsets = new int[FinalMissionSetRecord.CrimeSlotTotal];
                for (var slot = 0; slot < FinalMissionSetRecord.CrimeSlotTotal; slot++)
                {
                    if (!string.IsNullOrEmpty(ms.SlotStrings[slot]))
                    {
                        slotOffsets[slot] = tableBase + tableParts.Count;
                        tableParts.AddRange(Encoding.ASCII.GetBytes(ms.SlotStrings[slot]));
                        tableParts.Add(0);
                    }
                }
                // Record where null padding starts for this record's empty slots
                var padStart = tableBase + tableParts.Count;
                perRecordNullPadStart.Add(padStart);

                // Fill empty slot offsets into null padding area
                for (var slot = 0; slot < FinalMissionSetRecord.CrimeSlotTotal; slot++)
                {
                    if (string.IsNullOrEmpty(ms.SlotStrings[slot]))
                    {
                        slotOffsets[slot] = padStart; // will be set properly in ToBytes
                    }
                }
                perRecordSlotOffsets.Add(slotOffsets);

                // Add null padding to match original layout
                for (var p = 0; p < ms.TrailingPadding; p++)
                {
                    tableParts.Add(0);
                }
            }

            var stringTableBytes = tableParts.ToArray();

            // Build mission set record bytes with computed pointer words
            var missionSetBytes = new byte[MissionSets.Length * FinalMissionSetRecord.RecordSize];
            for (var i = 0; i < MissionSets.Length; i++)
            {
                Array.Copy(
                    MissionSets[i].ToBytes(perRecordSlotOffsets[i], perRecordNullPadStart[i]),
                    0, missionSetBytes, i * FinalMissionSetRecord.RecordSize,
                    FinalMissionSetRecord.RecordSize);
            }

            var crimeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CrimeTypeNames, CrimeTypeNameByteSizes);
            var orgBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(OrganisationNames, OrganisationNameByteSizes);
            var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

            // Compute character name pointer values
            var charNamesBaseOffset = PreStringTableData.Length + stringTableBytes.Length
                + PostStringTableData.Length + MissionSetParameters.Length
                + Unknown1.Length + missionSetBytes.Length + PostMissionPreCrimeData.Length
                + crimeBytes.Length + Unknown2.Length + orgBytes.Length
                + PostOrgPreCharNameData.Length;
            var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);

            return DataSegmentHelper.Concatenate(
                PreStringTableData,
                stringTableBytes,
                PostStringTableData,
                MissionSetParameters,
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
                MissionSetParameters = MissionSetParameters.ToArray(),
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
