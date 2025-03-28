using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core
{
    public static class ServiceCollectionExtension
    {
        public static void AddCovertActionsTools(this IServiceCollection services)
        {
            services.AddSingleton<IImporterFactory, ImporterFactory>();
            services.AddSingleton<IExporterFactory, ExporterFactory>();
            services.AddSingleton<ISimpleImageParser, SimpleImageParser>();
                             
            services.AddTransient<IImporter, FolderImporter>();
            services.AddTransient<IExporter, PackageExporter>();
        }
    }
}