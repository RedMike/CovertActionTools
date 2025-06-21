namespace CovertActionTools.Core.Utilities
{
    internal interface ICloneable<out TObject>
    {
        TObject Clone();
    }
}