using System.Collections.Generic;

namespace CovertActionTools.Core.Importing
{
    public struct ImportStatus
    {
        public enum ImportStage
        {
            Unknown = -1,
            ReadingIndex = 0,
            ProcessingSimpleImages = 10,
            ImportDone = 100,
            
            FatalError = 999999,
        }
        
        public ImportStage Stage { get; set; }
        public string StageMessage { get; set; }
        public int StageItems { get; set; }
        public int StageItemsDone { get; set; }
        public IReadOnlyList<string> Errors { get; set; }
    }
}