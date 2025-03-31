using System.Collections.Generic;

namespace CovertActionTools.Core.Exporting
{
    public struct ExportStatus
    {
        public enum ExportStage
        {
            Unknown = -1,
            Preparing = 0,
            ProcessingSimpleImages = 10,
            ProcessingCrimes = 20,
            ExportDone = 100,
            
            FatalError = 999999,
        }
        
        public ExportStage Stage { get; set; }
        public string StageMessage { get; set; }
        public int StageItems { get; set; }
        public int StageItemsDone { get; set; }
        public IReadOnlyList<string> Errors { get; set; }
    }
}