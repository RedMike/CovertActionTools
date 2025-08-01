﻿using System.Collections.Generic;
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
            /// To identify the same organisation across multiple World instances
            /// When set to 0xFF, mastermind is not allowed to join
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