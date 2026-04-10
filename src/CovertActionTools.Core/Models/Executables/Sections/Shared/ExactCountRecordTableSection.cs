using System.Collections.Generic;
using CovertActionTools.Core.Models.Executables.Records;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Null-separated list of records with no sentinel or count field which limits the ability to predict how
    /// many records should be in the list. This means the number of entries in the section is not allowed to
    /// change as it is dependent on the references from the Code Segment.
    /// The specific implementation dictates how many records there are.
    /// </summary>
    public abstract class ExactCountRecordTableSection<TRecord> : IExecutableSection
        where TRecord : IExecutableRecord, new()
    {
        /// <summary>
        /// Number of records in the list; must match exactly
        /// </summary>
        protected abstract int RecordCount { get; }

        public List<TRecord> Records { get; set; } = new();

        public abstract bool Viewable();
        public abstract bool Editable();

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var records = new List<TRecord>();
            var offset = startingOffset;
            while (records.Count < RecordCount)
            {
                var record = new TRecord();
                offset += record.ReadBytes(fullPayload, offset);
                records.Add(record);
            }

            Records = records;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var bytes = new List<byte>();
            foreach (var record in Records)
            {
                bytes.AddRange(record.WriteBytes());
            }

            return bytes.ToArray();
        }
    }
}