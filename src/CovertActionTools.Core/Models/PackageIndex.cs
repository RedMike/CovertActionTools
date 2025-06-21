using System;
using System.Collections.Generic;
using CovertActionTools.Core.Utilities;

namespace CovertActionTools.Core.Models
{
    public class PackageIndex : ICloneable<PackageIndex>
    {
        /// <summary>
        /// Serialisation version.
        /// Used to decide if the package can be loaded or not, and if updates need to happen.
        /// </summary>
        public int FormatVersion { get; set; }
        /// <summary>
        /// Author-decided package version, in SemVer format.
        /// </summary>
        public Version PackageVersion { get; set; } = new();
        public SharedMetadata Metadata { get; set; } = new();
        
        #region Diff Tracking
        //Changes means there are changes from the originals in the package
        public HashSet<string> SimpleImageChanges { get; set; } = new();
        public HashSet<int> CrimeChanges { get; set; } = new();
        public bool TextChanges { get; set; }
        public bool ClueChanges { get; set; }
        public bool PlotChanges { get; set; }
        public HashSet<int> WorldChanges { get; set; } = new();
        public HashSet<string> AnimationChanges { get; set; } = new();
        public HashSet<string> CatalogChanges { get; set; } = new();
        public bool FontChanges { get; set; }
        public bool ProseChanges { get; set; }
        
        //Included means the changes will be included during publishing
        public HashSet<string> SimpleImageIncluded { get; set; } = new();
        public HashSet<int> CrimeIncluded { get; set; } = new();
        public bool TextIncluded { get; set; }
        public bool ClueIncluded { get; set; }
        public bool PlotIncluded { get; set; }
        public HashSet<int> WorldIncluded { get; set; } = new();
        public HashSet<string> AnimationIncluded { get; set; } = new();
        public HashSet<string> CatalogIncluded { get; set; } = new();
        public bool FontIncluded { get; set; }
        public bool ProseIncluded { get; set; }
        #endregion

        public PackageIndex Clone()
        {
            return new PackageIndex()
            {
                FormatVersion = FormatVersion,
                PackageVersion = PackageVersion,
                Metadata = Metadata.Clone(),
                SimpleImageChanges = new HashSet<string>(SimpleImageChanges),
                SimpleImageIncluded = new HashSet<string>(SimpleImageIncluded),
                CrimeChanges = new HashSet<int>(CrimeChanges),
                CrimeIncluded = new HashSet<int>(CrimeIncluded),
                TextChanges = TextChanges,
                TextIncluded = TextIncluded,
                ClueChanges = ClueChanges,
                ClueIncluded = ClueIncluded,
                PlotChanges = PlotChanges,
                PlotIncluded = PlotIncluded,
                WorldChanges = new HashSet<int>(WorldChanges),
                WorldIncluded = new HashSet<int>(WorldIncluded),
                AnimationChanges = new HashSet<string>(AnimationChanges),
                AnimationIncluded = new HashSet<string>(AnimationIncluded),
                CatalogChanges = new HashSet<string>(CatalogChanges),
                CatalogIncluded = new HashSet<string>(CatalogIncluded),
                FontChanges = FontChanges,
                FontIncluded = FontIncluded,
                ProseChanges = ProseChanges,
                ProseIncluded = ProseIncluded
            };
        }
    }
}