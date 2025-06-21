using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing
{
    public interface IPackageImporter
    {
        bool CheckIfValidForImport(string path);
        void StartImport(string path);
        ImportStatus? CheckStatus();
        PackageModel GetImportedModel();
    }
    
    public interface IPackageImporter<TImporter> : IPackageImporter
        where TImporter : IImporter
    {
        
    }
}