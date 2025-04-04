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
            services.AddSingleton<ILegacySimpleImageParser, LegacySimpleImageParser>();
            services.AddSingleton<ILegacyCrimeParser, LegacyCrimeParser>();
            services.AddSingleton<ILegacyTextParser, LegacyTextParser>();

            services.AddSingleton<IImporter<Dictionary<string, SimpleImageModel>>, SimpleImageImporter>();
            services.AddSingleton<IImporter<Dictionary<int, CrimeModel>>, CrimeImporter>();
            services.AddSingleton<IImporter<Dictionary<string, TextModel>>, TextImporter>();
            services.AddSingleton<IReadOnlyList<IImporter>>(s => 
                new IImporter[]
                {
                    s.GetRequiredService<IImporter<Dictionary<string, SimpleImageModel>>>(),
                    s.GetRequiredService<IImporter<Dictionary<int, CrimeModel>>>(),
                    s.GetRequiredService<IImporter<Dictionary<string, TextModel>>>(),
                }
            );
            
            services.AddSingleton<ISimpleImageExporter, SimpleImageExporter>();
            services.AddSingleton<ICrimeExporter, CrimeExporter>();
            services.AddSingleton<ITextExporter, TextExporter>();
            
            services.AddTransient<LegacyFolderImporter>();
            services.AddTransient<IPackageImporter, PackageImporter>();
            services.AddTransient<IPackageExporter, PackageExporter>();
        }
    }
}