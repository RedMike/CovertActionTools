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

        /// <summary>Mission set plot strings (victim names, item names, etc.).</summary>
        public string[] Strings { get; set; } = Array.Empty<string>();

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
                Strings = Strings.Select(s => s).ToArray()
            };
        }

        public static FinalMissionSetRecord FromBytes(byte[] data, int offset, byte[] dataSegment)
        {
            var nameBytes = new byte[NameLength];
            Array.Copy(data, offset, nameBytes, 0, NameLength);
            var nameEnd = Array.IndexOf(nameBytes, (byte)0);
            if (nameEnd < 0) nameEnd = NameLength;
            var name = Encoding.ASCII.GetString(nameBytes, 0, nameEnd);

            var ptrWords = DataSegmentHelper.BytesToUInt16Array(data, offset + 0x2A, StringPointerCount);

            // Extract strings from (start, end) pointer pairs
            var strings = new List<string>();
            var lastEndPtr = 0;
            for (var i = 0; i < ptrWords.Length - 1; i++)
            {
                var start = ptrWords[i];
                var end = ptrWords[i + 1];
                if (end <= start || start >= dataSegment.Length || end > dataSegment.Length) break;
                var strEnd = start;
                while (strEnd < dataSegment.Length && strEnd < end && dataSegment[strEnd] != 0) strEnd++;
                if (strEnd > start)
                {
                    strings.Add(Encoding.ASCII.GetString(dataSegment, start, strEnd - start));
                    lastEndPtr = ptrWords[i + 1];
                }
                else
                {
                    break;
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
                Strings = strings.ToArray()
            };
        }

        public byte[] ToBytes(ushort[] stringPointerWords)
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
            var ptrBytes = DataSegmentHelper.UInt16ArrayToBytes(stringPointerWords);
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

        /// <summary>Data before mission set params: MSC runtime, BSS, RastPort, CGA, strings, mission set string table.</summary>
        public byte[] PreMissionParamData { get; set; } = Array.Empty<byte>();

        /// <summary>Offset of the mission set string table within PreMissionParamData.</summary>
        public int StringTableOffset { get; set; }

        /// <summary>Length of the mission set string table within PreMissionParamData.</summary>
        public int StringTableLength { get; set; }

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

            // Store the full pre-param block as-is for byte-identical roundtrip.
            // Record the string table location within it for editing support.
            segment.PreMissionParamData = DataSegmentHelper.Slice(dataSegment, 0, MissionParamsOffset);

            // Find string table boundaries from mission set pointer words
            var tempContentPtrs = new List<int>();
            for (var i = 0; i < MissionSetCount; i++)
            {
                var recBase = MissionSetsOffset + i * FinalMissionSetRecord.RecordSize + 0x2A;
                for (var w = 0; w < FinalMissionSetRecord.StringPointerCount; w++)
                {
                    var ptr = BitConverter.ToUInt16(dataSegment, recBase + w * 2);
                    if (ptr > 0 && ptr < dataSegment.Length && dataSegment[ptr] != 0)
                    {
                        tempContentPtrs.Add(ptr);
                    }
                }
            }
            if (tempContentPtrs.Count > 0)
            {
                segment.StringTableOffset = tempContentPtrs.Min();
                var maxEnd = 0;
                foreach (var ptr in tempContentPtrs)
                {
                    var e = ptr;
                    while (e < dataSegment.Length && dataSegment[e] != 0) e++;
                    e++;
                    if (e > maxEnd) maxEnd = e;
                }
                segment.StringTableLength = maxEnd - segment.StringTableOffset;
            }

            segment.MissionSetParameters = DataSegmentHelper.Slice(dataSegment, MissionParamsOffset, MissionParamsSize);

            var missionSetsStart = MissionParamsOffset + MissionParamsSize;
            segment.Unknown1 = DataSegmentHelper.Slice(dataSegment, missionSetsStart, MissionSetsOffset - missionSetsStart);

            segment.MissionSets = new FinalMissionSetRecord[MissionSetCount];
            for (var i = 0; i < MissionSetCount; i++)
            {
                segment.MissionSets[i] = FinalMissionSetRecord.FromBytes(dataSegment, MissionSetsOffset + i * FinalMissionSetRecord.RecordSize, dataSegment);
            }

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
            // The string table lives within PreMissionParamData at StringTableOffset.
            // For unmodified data, we write PreMissionParamData as-is and compute pointer words
            // by walking the raw bytes. When strings are edited, we modify the table in-place.
            var preMissionBytes = PreMissionParamData.ToArray();
            if (StringTableLength > 0)
            {
                // Compute pointer words for each record by walking the string table bytes.
                // Algorithm: for each record, emit start of its first string, then for each
                // subsequent word: if byte is non-null, skip to byte after null; else advance by 1.
                // The walk position for each record starts where that record's strings begin in
                // the table — determined by advancing past all previous records' strings.
                var perRecordPtrWords = new List<ushort[]>();
                var tablePos = StringTableOffset;
                foreach (var ms in MissionSets)
                {
                    var words = new ushort[FinalMissionSetRecord.StringPointerCount];
                    words[0] = (ushort)tablePos;
                    var pos = tablePos;
                    for (var w = 1; w < words.Length; w++)
                    {
                        if (pos < preMissionBytes.Length && preMissionBytes[pos] != 0)
                        {
                            while (pos < preMissionBytes.Length && preMissionBytes[pos] != 0) pos++;
                            pos++;
                        }
                        else
                        {
                            pos++;
                        }
                        words[w] = (ushort)pos;
                    }
                    perRecordPtrWords.Add(words);

                    // Advance tablePos to next record's first string.
                    // The last pointer word tells us where the walk ended. Skip past remaining
                    // null padding to find the next non-null byte (next record's first string).
                    var walkEnd = (int)words[words.Length - 1];
                    // walkEnd might be in null padding or string content. Advance past it.
                    walkEnd++; // past the last word's position
                    while (walkEnd < preMissionBytes.Length && preMissionBytes[walkEnd] == 0) walkEnd++;
                    tablePos = walkEnd;
                }

                var missionSetBytes = new byte[MissionSets.Length * FinalMissionSetRecord.RecordSize];
                for (var i = 0; i < MissionSets.Length; i++)
                {
                    Array.Copy(MissionSets[i].ToBytes(perRecordPtrWords[i]), 0, missionSetBytes,
                        i * FinalMissionSetRecord.RecordSize, FinalMissionSetRecord.RecordSize);
                }

                var crimeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CrimeTypeNames, CrimeTypeNameByteSizes);
                var orgBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(OrganisationNames, OrganisationNameByteSizes);
                var charNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);

                var charNamesBaseOffset = preMissionBytes.Length + MissionSetParameters.Length
                    + Unknown1.Length + missionSetBytes.Length + PostMissionPreCrimeData.Length
                    + crimeBytes.Length + Unknown2.Length + orgBytes.Length
                    + PostOrgPreCharNameData.Length;
                var charNamePointers = DataSegmentHelper.ComputeStringPointers(CharacterNames, charNamesBaseOffset);

                return DataSegmentHelper.Concatenate(
                    preMissionBytes, MissionSetParameters, Unknown1, missionSetBytes,
                    PostMissionPreCrimeData, crimeBytes, Unknown2, orgBytes,
                    PostOrgPreCharNameData, charNamesBytes, PostCharNameData,
                    DataSegmentHelper.UInt16ArrayToBytes(charNamePointers), TrailingData
                );
            }

            // Fallback: no string table (shouldn't happen for valid FINAL data)
            var fallbackMsBytes = new byte[MissionSets.Length * FinalMissionSetRecord.RecordSize];
            for (var i = 0; i < MissionSets.Length; i++)
            {
                Array.Copy(MissionSets[i].ToBytes(new ushort[FinalMissionSetRecord.StringPointerCount]),
                    0, fallbackMsBytes, i * FinalMissionSetRecord.RecordSize, FinalMissionSetRecord.RecordSize);
            }

            var fbCrimeBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(CrimeTypeNames, CrimeTypeNameByteSizes);
            var fbOrgBytes = DataSegmentHelper.NullTerminatedStringsToFixedBytes(OrganisationNames, OrganisationNameByteSizes);
            var fbCharNamesBytes = DataSegmentHelper.NullTerminatedStringsToBytes(CharacterNames);
            var fbCharBase = preMissionBytes.Length + MissionSetParameters.Length
                + Unknown1.Length + fallbackMsBytes.Length + PostMissionPreCrimeData.Length
                + fbCrimeBytes.Length + Unknown2.Length + fbOrgBytes.Length
                + PostOrgPreCharNameData.Length;
            var fbCharPtrs = DataSegmentHelper.ComputeStringPointers(CharacterNames, fbCharBase);

            return DataSegmentHelper.Concatenate(
                preMissionBytes, MissionSetParameters, Unknown1, fallbackMsBytes,
                PostMissionPreCrimeData, fbCrimeBytes, Unknown2, fbOrgBytes,
                PostOrgPreCharNameData, fbCharNamesBytes, PostCharNameData,
                DataSegmentHelper.UInt16ArrayToBytes(fbCharPtrs), TrailingData
            );
        }

        public FinalDataSegment Clone()
        {
            return new FinalDataSegment
            {
                PreMissionParamData = PreMissionParamData.ToArray(),
                StringTableOffset = StringTableOffset,
                StringTableLength = StringTableLength,
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
