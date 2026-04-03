using System;
using System.Linq;
using System.Text;

namespace CovertActionTools.Core.Models.Executables
{
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

        /// <summary>16 × 26-byte mission set parameter records (values 0-8, partially understood).</summary>
        public byte[] MissionSetParameters { get; set; } = Array.Empty<byte>();

        /// <summary>2-byte gap between params and mission set records.</summary>
        public byte[] Unknown1 { get; set; } = Array.Empty<byte>();

        /// <summary>16 mission set records (74 bytes each): name, crime IDs, flags, string pointers.</summary>
        public FinalMissionSetRecord[] MissionSets { get; set; } = Array.Empty<FinalMissionSetRecord>();

        /// <summary>Data between mission sets and crime types: plot/briefing strings, skill names, case text.</summary>
        public byte[] PostMissionPreCrimeData { get; set; } = Array.Empty<byte>();

        /// <summary>13 crime type name strings (null-terminated, variable length).</summary>
        public byte[] CrimeTypeNames { get; set; } = Array.Empty<byte>();

        /// <summary>Data between crime type names end and organisation names start.</summary>
        public byte[] Unknown2 { get; set; } = Array.Empty<byte>();

        /// <summary>24 organisation name strings (null-terminated, variable length).</summary>
        public byte[] OrganisationNames { get; set; } = Array.Empty<byte>();

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
            segment.CrimeTypeNames = DataSegmentHelper.Slice(dataSegment, CrimeTypesOffset, crimeEnd - CrimeTypesOffset);

            segment.Unknown2 = DataSegmentHelper.Slice(dataSegment, crimeEnd, OrgsOffset - crimeEnd);

            // Organisation names: 24 null-terminated strings
            var orgEnd = FindNthNullTerminator(dataSegment, OrgsOffset, OrgCount);
            segment.OrganisationNames = DataSegmentHelper.Slice(dataSegment, OrgsOffset, orgEnd - OrgsOffset);

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
                CrimeTypeNames,
                Unknown2,
                OrganisationNames,
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
                CrimeTypeNames = CrimeTypeNames.ToArray(),
                Unknown2 = Unknown2.ToArray(),
                OrganisationNames = OrganisationNames.ToArray(),
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
