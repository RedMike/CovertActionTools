using System.Collections.Generic;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core
{
    public static class ServiceCollectionExtension
    {
        public static void AddCovertActionsTools(this IServiceCollection services)
        {
            services.AddSingleton<LegacySimpleImageParser>();
            services.AddSingleton<LegacyCrimeParser>();
            services.AddSingleton<LegacyTextParser>();

            services.AddSingleton<IImporter<Dictionary<string, SimpleImageModel>>, SimpleImageImporter>();
            services.AddSingleton<IImporter<Dictionary<int, CrimeModel>>, CrimeImporter>();
            services.AddSingleton<IImporter<Dictionary<string, TextModel>>, TextImporter>();
            
            services.AddSingleton<IExporter<Dictionary<string, SimpleImageModel>>, SimpleImageExporter>();
            services.AddSingleton<IExporter<Dictionary<int, CrimeModel>>, CrimeExporter>();
            services.AddSingleton<IExporter<Dictionary<string, TextModel>>, TextExporter>();
            
            services.AddTransient<LegacyFolderImporter>();
            services.AddTransient<IPackageImporter, PackageImporter>();
            services.AddTransient<IPackageExporter, PackageExporter>();
        }
    }
}