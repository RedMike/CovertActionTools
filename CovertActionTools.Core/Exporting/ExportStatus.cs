using System.Collections.Generic;

namespace CovertActionTools.Core.Exporting
{
    public struct ExportStatus
    {
        public int StageCount { get; set; }
        public int StagesDone { get; set; }
        public string StageMessage { get; set; }
        public int StageItems { get; set; }
        public int StageItemsDone { get; set; }
        public IReadOnlyList<string> Errors { get; set; }
        public bool Done { get; set; }
        
        public float GetProgress()
        {
            var progress = 1.0f;
            if (StageCount > 0)
            {
                var increment = (1.0f / StageCount); 
                progress = increment * StagesDone;
                if (StageItems > 0)
                {
                    progress += (StageItemsDone / (float)StageItems) * increment;
                }
            }

            return progress;
        }
    }
}