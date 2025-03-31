using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Importing.Importers;
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
            services.AddSingleton<ILegacyCrimeParser, LegacyCrimeParser>();
            services.AddSingleton<ISimpleImageImporter, SimpleImageImporter>();
            services.AddSingleton<ISimpleImageExporter, SimpleImageExporter>();
            services.AddSingleton<ICrimeExporter, CrimeExporter>();
            
            services.AddTransient<LegacyFolderImporter>();
            services.AddTransient<IImporter, PackageImporter>();
            services.AddTransient<IExporter, PackageExporter>();
        }
    }
}