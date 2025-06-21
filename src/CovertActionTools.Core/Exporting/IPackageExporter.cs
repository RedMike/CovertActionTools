using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Exporting
{
    public interface IPackageExporter
    {
        void StartExport(PackageModel model, string path);
        ExportStatus? CheckStatus();
    }

    public interface IPackageExporter<TExporter> : IPackageExporter
        where TExporter : IExporter
    {
        
    }
}