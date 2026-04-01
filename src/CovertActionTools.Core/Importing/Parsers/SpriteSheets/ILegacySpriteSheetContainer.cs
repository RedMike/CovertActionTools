using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal interface ILegacySpriteSheetContainer
    {
        bool TryGet(string key, out SimpleImageModel.SpriteSheetData data);
    }
}
