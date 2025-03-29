using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Exporting.Exporters;
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
            
            services.AddSingleton<ILegacySimpleImageParser, LegacySimpleImageParser>();
            services.AddSingleton<ISimpleImageExporter, SimpleImageExporter>();
            
            services.AddTransient<LegacyFolderImporter>();
            services.AddTransient<IImporter, LegacyFolderImporter>();
            services.AddTransient<IExporter, PackageExporter>();
        }
    }
}