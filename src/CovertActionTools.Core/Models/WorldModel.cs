using System.Collections.Generic;
using System.Linq;

namespace CovertActionTools.Core.Models
{
    public class WorldModel
    {
        public class City
        {
            /// <summary>
            /// Printed name
            /// Legacy limited to 12 chars
            /// </summary>
            public string Name { get; set; } = string.Empty;

            /// <summary>
            /// Printed name
            /// Legacy limited to 12 chars
            /// </summary>
            public string Country { get; set; } = string.Empty;
            
            /// <summary>
            /// TODO: ?
            /// </summary>
            public int Unknown1 { get; set; }
            /// <summary>
            /// TODO: ?
            /// </summary>
            public int Unknown2 { get; set; }
            
            /// <summary>
            /// X coord on map, also used to calculate travel time.
            /// TODO: clarify direction
            /// </summary>
            public int MapX { get; set; }
            /// <summary>
            /// Y coord on map, also used to calculate travel time.
            /// TODO: clarify direction
            /// </summary>
            public int MapY { get; set; }
        }

        public class Organisation
        {
            /// <summary>
            /// Printed short name
            /// Legacy limited to 6 chars
            /// </summary>
            public string ShortName { get; set; } = string.Empty;

            /// <summary>
            /// Printed long name
            /// Legacy limited to 20 chars
            /// </summary>
            public string LongName { get; set; } = string.Empty;
            
            /// <summary>
            /// TODO: ?
            /// </summary>
            public int Unknown1 { get; set; }
            /// <summary>
            /// TODO: ?
            /// </summary>
            public int Unknown2 { get; set; }
            /// <summary>
            /// TODO: ?
            /// </summary>
            public int Unknown3 { get; set; }
            /// <summary>
            /// Org alliance membership — packed 16-bit field at record offset +0x20.
            /// High byte: org type bitmask (4 bits used: 0x01, 0x02, 0x04, 0x08) that controls
            /// which mission sets this org is eligible for. FINAL.EXE FUN_1100_0734 ANDs this
            /// against FinalMissionSetRecord.OrgTypeMask (shifted into the high byte of a word at
            /// record +0x18) to filter org/mission compatibility.
            /// Low byte: unique org ID (0-25) used to index per-org parameter records in FINAL.EXE
            /// at DS+0x1CFC (stride 16). Shared across WORLD files to identify the same org.
            /// Value 0xFFFF marks allied orgs (cops, CIA, MI6, Mossad, KGB) — rejected by the
            /// mission selection loop and excluded from mastermind selection.
            /// TODO: decode the 4 org type bits into named categories (see Ghidra disassembly
            /// in scratch/ghidra/output/FINAL_mission_decompiled.json, FUN_1100_0734).
            /// </summary>
            public int UniqueId { get; set; }

            public bool AllowMastermind => (UniqueId & 0xFF) != 0xFF;
            
            /// <summary>
            /// TODO: ?
            /// </summary>
            public int Unknown4 { get; set; }
        }

        public int Id { get; set; }
        public List<City> Cities { get; set; } = new();
        public List<Organisation> Organisations { get; set; } = new();
        public SharedMetadata Metadata { get; set; } = new();

        public WorldModel Clone()
        {
            return new WorldModel()
            {
                Id = Id,
                Cities = Cities.Select(x => new City()
                {
                    Country = x.Country,
                    MapX = x.MapX,
                    MapY = x.MapY,
                    Name = x.Name,
                    Unknown1 = x.Unknown1,
                    Unknown2 = x.Unknown2
                }).ToList(),
                Organisations = Organisations.Select(x => new Organisation()
                {
                    UniqueId = x.UniqueId,
                    ShortName = x.ShortName,
                    LongName = x.LongName,
                    Unknown1 = x.Unknown1,
                    Unknown2 = x.Unknown2,
                    Unknown3 = x.Unknown3,
                    Unknown4 = x.Unknown4
                }).ToList(),
                Metadata = Metadata.Clone()
            };
        }
    }
}