using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing.Parsers.SpriteSheets
{
    internal abstract class BaseLegacySpriteSheetData
    {
        public abstract string[] Keys { get; }
        public abstract SimpleImageModel.SpriteSheetData Create();
    }
}
