using System.Linq;

namespace CovertActionTools.Core.Models.Executables.Sections.Shared
{
    public abstract class ExactCountFixedSizeStringTableSection : ExactCountStringTableSection
    {
        protected abstract int[] StringSizes { get; }

        protected sealed override int StringCount => StringSizes.Length;
        protected sealed override int? StringLength => null;

        public override bool Viewable()
        {
            return true;
        }

        public override bool Editable()
        {
            return true;
        }

        public override int ReadBytes(byte[] fullPayload, int startingOffset)
        {
            Strings = new System.Collections.Generic.List<string>();
            var pos = startingOffset;
            for (var i = 0; i < StringSizes.Length; i++)
            {
                var fieldSize = StringSizes[i];
                var strLen = 0;
                while (strLen < fieldSize && fullPayload[pos + strLen] != 0) strLen++;
                Strings.Add(DataSegmentHelper.DecodeControlString(fullPayload, pos, strLen));
                pos += fieldSize;
            }
            return pos - startingOffset;
        }

        public override byte[] WriteBytes()
        {
            var totalSize = 0;
            foreach (var s in StringSizes) totalSize += s;
            var result = new byte[totalSize];
            var pos = 0;
            for (var i = 0; i < StringSizes.Length; i++)
            {
                var fieldSize = StringSizes[i];
                if (i < Strings.Count)
                {
                    var encoded = DataSegmentHelper.EncodeControlString(Strings[i]);
                    var copyLen = System.Math.Min(encoded.Length, fieldSize - 1);
                    System.Array.Copy(encoded, 0, result, pos, copyLen);
                }
                pos += fieldSize;
            }
            return result;
        }
    }
}
