using System.Collections.Generic;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    /// <summary>
    /// Null-separated list of strings with no sentinel or count field which limits the ability to predict how
    /// many strings should be in the list. This means the number of entries in the section is not allowed to
    /// change as it is dependent on the references from the Code Segment.
    /// The specific implementation dictates how many strings there are.
    /// </summary>
    public abstract class ExactCountStringTableSection : IExecutableSection
    {
        /// <summary>
        /// Number of strings in the list; must match exactly
        /// </summary>
        protected abstract int StringCount { get; }
        /// <summary>
        /// If set, indicates an exact length of string which is padded out; should not be set unless it's fixed size
        /// </summary>
        protected abstract int? StringLength { get; }

        protected List<string> Strings { get; set; } = new();

        public abstract bool Viewable();
        public abstract bool Editable();

        public int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            var s = new List<string>();
            var offset = startingOffset;
            while (s.Count < StringCount)
            {
                int stringLength = 0;
                if (StringLength == null)
                {
                    while (fullPayload[offset + stringLength] != 0)
                    {
                        stringLength++;
                    }
                }
                else
                {
                    stringLength = StringLength.Value;
                }

                var str = DataSegmentHelper.DecodeControlString(fullPayload, offset, stringLength);
                offset += stringLength + 1;
                s.Add(str);
            }

            Strings = s;
            return offset - startingOffset;
        }

        public byte[] WriteBytes()
        {
            var bytes = new List<byte>();
            foreach (var str in Strings)
            {
                var strBytes = DataSegmentHelper.EncodeControlString(str);
                bytes.AddRange(strBytes);
                if (StringLength != null)
                {
                    //we don't rely on a null terminator, but it's a fixed size
                    var paddingLength = StringLength.Value - strBytes.Length;
                    if (paddingLength > 0)
                    {
                        for (var i = 0; i < paddingLength; i++)
                        {
                            bytes.Add(0);
                        }
                    }
                }
                else
                {
                    bytes.Add(0); //null terminator
                }
            }

            return bytes.ToArray();
        }
    }
}