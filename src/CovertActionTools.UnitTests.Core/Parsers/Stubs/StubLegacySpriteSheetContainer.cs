using System.Collections.Generic;
using CovertActionTools.Core.Importing.Parsers.SpriteSheets;
using CovertActionTools.Core.Models;

namespace CovertActionTools.UnitTests.Core.Parsers.Stubs
{
    internal class StubLegacySpriteSheetContainer : ILegacySpriteSheetContainer
    {
        private readonly Dictionary<string, SimpleImageModel.SpriteSheetData> _data = new Dictionary<string, SimpleImageModel.SpriteSheetData>();

        public void Set(string key, SimpleImageModel.SpriteSheetData data)
        {
            _data[key] = data;
        }

        public bool TryGet(string key, out SimpleImageModel.SpriteSheetData data)
        {
            if (_data.TryGetValue(key, out var spriteSheet))
            {
                data = spriteSheet.Clone();
                return true;
            }

            data = null;
            return false;
        }
    }
}
