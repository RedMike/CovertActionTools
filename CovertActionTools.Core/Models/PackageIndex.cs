using System;
using System.Collections.Generic;

namespace CovertActionTools.Core.Models
{
    public class PackageIndex
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
        public HashSet<string> SimpleImageChanges { get; set; } = new();
        public HashSet<int> CrimeChanges { get; set; } = new();
        public bool TextChanges { get; set; }
        public bool ClueChanges { get; set; }
        public HashSet<int> WorldChanges { get; set; } = new();
        public HashSet<string> AnimationChanges { get; set; } = new();
        public HashSet<string> CatalogChanges { get; set; } = new();
        public bool FontChanges { get; set; }
        public bool ProseChanges { get; set; }
        #endregion
    }
}