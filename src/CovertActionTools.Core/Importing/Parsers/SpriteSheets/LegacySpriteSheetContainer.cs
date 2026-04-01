using System.Collections.Generic;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal class LegacySpriteSheetContainer : ILegacySpriteSheetContainer
    {
        private readonly Dictionary<string, SimpleImageModel.SpriteSheetData> _spriteSheets = new Dictionary<string, SimpleImageModel.SpriteSheetData>();

        public LegacySpriteSheetContainer(IEnumerable<BaseLegacySpriteSheetData> spriteSheetDataProviders)
        {
            foreach (var provider in spriteSheetDataProviders)
            {
                var data = provider.Create();
                foreach (var key in provider.Keys)
                {
                    _spriteSheets[key] = data;
                }
            }
        }

        public bool TryGet(string key, out SimpleImageModel.SpriteSheetData data)
        {
            if (_spriteSheets.TryGetValue(key, out var spriteSheet))
            {
                data = spriteSheet.Clone();
                return true;
            }

            data = null;
            return false;
        }
    }
}
