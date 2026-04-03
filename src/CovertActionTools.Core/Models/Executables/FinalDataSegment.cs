using System;
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

        /// <summary>16 DS-relative string pointer pairs into victim/item string table. Not editable (pointers).</summary>
        public ushort[] StringPointers { get; set; } = Array.Empty<ushort>();

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
                StringPointers = StringPointers.ToArray()
            };
        }

        public static FinalMissionSetRecord FromBytes(byte[] data, int offset)
        {
            var nameBytes = new byte[NameLength];
            Array.Copy(data, offset, nameBytes, 0, NameLength);
            var nameEnd = Array.IndexOf(nameBytes, (byte)0);
            if (nameEnd < 0) nameEnd = NameLength;
            var name = Encoding.ASCII.GetString(nameBytes, 0, nameEnd);

            return new FinalMissionSetRecord
            {
                Name = name,
                Unknown1 = data[offset + 0x19],
                Crime1Id = BitConverter.ToUInt16(data, offset + 0x1A),
                Crime2Id = BitConverter.ToUInt16(data, offset + 0x1C),
                Crime3Id = BitConverter.ToUInt16(data, offset + 0x1E),
                UnusedCrimeSlots = DataSegmentHelper.Slice(data, offset + 0x20, UnusedSlotCount),
                FlagWord = BitConverter.ToUInt16(data, offset + 0x28),
                StringPointers = DataSegmentHelper.BytesToUInt16Array(data, offset + 0x2A, StringPointerCount)
            };
        }

        public byte[] ToBytes()
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
            var ptrBytes = DataSegmentHelper.UInt16ArrayToBytes(StringPointers);
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
        private const int PostMissionParamsGap = 2;        // 2-byte gap before mission set records
        private const int MissionSetsOffset = 0x1E9E;      // 0x012C1E - 0x10D80
        private const int MissionSetCount = 16;
        private const int CrimeTypesOffset = 0x2AFC;        // 0x01387C - 0x10D80
        private const int CrimeTypeCount = 13;
        private const int OrgsOffset = 0x2B88;              // 0x013908 - 0x10D80
        private const int OrgCount = 24;
        private const int CharNamePointersOffset = 0x4526;  // 0x0152A6 - 0x10D80
        private const int CharNamePointerCount = 192;
        #endregion

        #region Fields (in binary order)

        /// <summary>Data before mission set params: MSC runtime, BSS, RastPort, CGA, strings.</summary>
        public byte[] PreMissionParamData { get; set; } = Array.Empty<byte>();

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

        /// <summary>Data between crime type names end and organisation names start.</summary>
        public byte[] Unknown2 { get; set; } = Array.Empty<byte>();

        /// <summary>24 organisation name strings.</summary>
        public string[] OrganisationNames { get; set; } = Array.Empty<string>();

        /// <summary>Data between org names and character name pointers: career text, briefing, clue phrases, char names, item tables.</summary>
        public byte[] PostOrgPreCharPtrData { get; set; } = Array.Empty<byte>();

        /// <summary>192 DS-relative pointers into character name strings.</summary>
        public ushort[] CharacterNamePointers { get; set; } = Array.Empty<ushort>();

        /// <summary>Everything after character name pointers: game state, file management, runtime, BSS.</summary>
        public byte[] TrailingData { get; set; } = Array.Empty<byte>();

        #endregion

        public static FinalDataSegment FromBytes(byte[] dataSegment)
        {
            var segment = new FinalDataSegment();

            segment.PreMissionParamData = DataSegmentHelper.Slice(dataSegment, 0, MissionParamsOffset);

            segment.MissionSetParameters = DataSegmentHelper.Slice(dataSegment, MissionParamsOffset, MissionParamsSize);

            var missionSetsStart = MissionParamsOffset + MissionParamsSize;
            segment.Unknown1 = DataSegmentHelper.Slice(dataSegment, missionSetsStart, MissionSetsOffset - missionSetsStart);

            segment.MissionSets = new FinalMissionSetRecord[MissionSetCount];
            for (var i = 0; i < MissionSetCount; i++)
            {
                segment.MissionSets[i] = FinalMissionSetRecord.FromBytes(dataSegment, MissionSetsOffset + i * FinalMissionSetRecord.RecordSize);
            }

            var missionSetsEnd = MissionSetsOffset + MissionSetCount * FinalMissionSetRecord.RecordSize;
            segment.PostMissionPreCrimeData = DataSegmentHelper.Slice(dataSegment, missionSetsEnd, CrimeTypesOffset - missionSetsEnd);

            // Crime type names: 13 null-terminated strings
            var crimeEnd = FindNthNullTerminator(dataSegment, CrimeTypesOffset, CrimeTypeCount);
            segment.CrimeTypeNames = DataSegmentHelper.NullTerminatedStringsFromBytes(dataSegment, CrimeTypesOffset, CrimeTypeCount);

            segment.Unknown2 = DataSegmentHelper.Slice(dataSegment, crimeEnd, OrgsOffset - crimeEnd);

            // Organisation names: 24 null-terminated strings
            var orgEnd = FindNthNullTerminator(dataSegment, OrgsOffset, OrgCount);
            segment.OrganisationNames = DataSegmentHelper.NullTerminatedStringsFromBytes(dataSegment, OrgsOffset, OrgCount);

            segment.PostOrgPreCharPtrData = DataSegmentHelper.Slice(dataSegment, orgEnd, CharNamePointersOffset - orgEnd);

            segment.CharacterNamePointers = DataSegmentHelper.BytesToUInt16Array(dataSegment, CharNamePointersOffset, CharNamePointerCount);

            var charPtrsEnd = CharNamePointersOffset + CharNamePointerCount * 2;
            segment.TrailingData = DataSegmentHelper.Slice(dataSegment, charPtrsEnd, dataSegment.Length - charPtrsEnd);

            return segment;
        }

        public byte[] ToBytes()
        {
            var missionSetBytes = new byte[MissionSetCount * FinalMissionSetRecord.RecordSize];
            for (var i = 0; i < MissionSets.Length; i++)
            {
                Array.Copy(MissionSets[i].ToBytes(), 0, missionSetBytes, i * FinalMissionSetRecord.RecordSize, FinalMissionSetRecord.RecordSize);
            }

            return DataSegmentHelper.Concatenate(
                PreMissionParamData,
                MissionSetParameters,
                Unknown1,
                missionSetBytes,
                PostMissionPreCrimeData,
                DataSegmentHelper.NullTerminatedStringsToBytes(CrimeTypeNames),
                Unknown2,
                DataSegmentHelper.NullTerminatedStringsToBytes(OrganisationNames),
                PostOrgPreCharPtrData,
                DataSegmentHelper.UInt16ArrayToBytes(CharacterNamePointers),
                TrailingData
            );
        }

        public FinalDataSegment Clone()
        {
            return new FinalDataSegment
            {
                PreMissionParamData = PreMissionParamData.ToArray(),
                MissionSetParameters = MissionSetParameters.ToArray(),
                Unknown1 = Unknown1.ToArray(),
                MissionSets = MissionSets.Select(m => m.Clone()).ToArray(),
                PostMissionPreCrimeData = PostMissionPreCrimeData.ToArray(),
                CrimeTypeNames = CrimeTypeNames.Select(s => s).ToArray(),
                Unknown2 = Unknown2.ToArray(),
                OrganisationNames = OrganisationNames.Select(s => s).ToArray(),
                PostOrgPreCharPtrData = PostOrgPreCharPtrData.ToArray(),
                CharacterNamePointers = CharacterNamePointers.ToArray(),
                TrailingData = TrailingData.ToArray()
            };
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
