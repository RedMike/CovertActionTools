using System.Linq;
using CovertActionTools.Core.Models.Executables.Sections.Shared;

namespace CovertActionTools.Core.Models.Executables.Sections.Tac
{
    public class PasswordGenerationSection : ExactCountFixedSizeStringTableSection
    {
        protected override int[] StringSizes => _stringSizes;
        private int[] _stringSizes = new[] { 3, 9, 5, 5, 11 };

        public string ReadFileMode
        {
            get => Strings.Count > 0 ? Strings[0] : "";
            set { if (Strings.Count > 0) Strings[0] = value; }
        }

        public string Filename
        {
            get => Strings.Count > 1 ? Strings[1] : "";
            set { if (Strings.Count > 1) Strings[1] = value; }
        }

        public string FileReadFormat1
        {
            get => Strings.Count > 2 ? Strings[2] : "";
            set { if (Strings.Count > 2) Strings[2] = value; }
        }

        public string FileReadFormat2
        {
            get => Strings.Count > 3 ? Strings[3] : "";
            set { if (Strings.Count > 3) Strings[3] = value; }
        }

        public string PasswordTemplateString
        {
            get => Strings.Count > 4 ? Strings[4] : "";
            set { if (Strings.Count > 4) Strings[4] = value; }
        }

        public override bool Viewable()
        {
            return false;
        }

        public override bool Editable()
        {
            return false;
        }

        public PasswordGenerationSection Clone()
        {
            return new PasswordGenerationSection
            {
                Strings = Strings.ToList()
            };
        }
    }
}
