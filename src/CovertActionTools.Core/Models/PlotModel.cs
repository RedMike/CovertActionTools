using System;

namespace CovertActionTools.Core.Models
{
    /// <summary>
    /// Saved as: PLxxyz, where xx is the mission set ID (from EXE), y is crime index (or 9 if briefing),
    /// z is a type identifier for the type of string combined with a sequential ID.
    /// This means that the mission set data is required in order to interpret this data entirely.
    /// TODO: does the `z` value for A and a matter if it's different? A number of them are 'duplicates'. Maybe failed completely (A) vs failed but stopped part of the crime (a)?
    /// </summary>
    public class PlotModel
    {
        public enum PlotStringType
        {
            Unknown = -1,
            Briefing = 0, //initial messages about a mission set before clues, xx90-xx94
            BriefingPreviousFailure = 1, //initial messages when previous crime was failed, xxyA-xxyF, upper and lower
            Success = 2, //messages at the end of a mission where success happens, xxy0-xxy4
            Failure = 3, //messages at the end of a mission where failure happens, xxy5-xxy9
        }
        
        public PlotStringType StringType { get; set; }
        /// <summary>
        /// Which mission set is running.
        /// Note: in legacy, this is from a list in the .EXE, not data
        /// </summary>
        public int MissionSetId { get; set; }
        /// <summary>
        /// The crime index that is running, from the mission set definition.
        /// Legacy values from 0 to 2, so 0 is the first crime in the set, etc.
        /// Not set when Briefing type.
        /// </summary>
        public int? CrimeIndex { get; set; }
        /// <summary>
        /// Sequential number within (MissionSetId, CrimeIndex, Type).
        /// Starts at 0. Maximum of 4. Actual character value written influenced by `Type`
        /// Legacy usually has max 3.
        /// </summary>
        public int MessageNumber { get; set; }
        public string Message { get; set; } = string.Empty;
        
        public string GetMessagePrefix()
        {
            switch (StringType)
            {
                case PlotStringType.Briefing:
                    return $"PL{MissionSetId:D2}9{MessageNumber:D1}";
                case PlotStringType.BriefingPreviousFailure:
                    return $"PL{MissionSetId:D2}{CrimeIndex:D1}{(MessageNumber + 10):X1}";
                case PlotStringType.Success:
                    return $"PL{MissionSetId:D2}{CrimeIndex:D1}{MessageNumber:D1}";
                case PlotStringType.Failure:
                    return $"PL{MissionSetId:D2}{CrimeIndex:D1}{(MessageNumber + 5):X1}";
                default:
                    throw new Exception($"Unknown type: {StringType}");
            }
        }

        public PlotModel Clone()
        {
            return new PlotModel()
            {
                MissionSetId = MissionSetId,
                CrimeIndex = CrimeIndex,
                Message = Message,
                MessageNumber = MessageNumber,
                StringType = StringType
            };
        }
    }
}