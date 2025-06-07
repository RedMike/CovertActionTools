using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Parsers;
using CovertActionTools.Core.Importing.Shared;
using CovertActionTools.Core.Models;
using CovertActionTools.Core.Processors;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core
{
    public static class ServiceCollectionExtension
    {
        public static void AddCovertActionsTools(this IServiceCollection services)
        {
            services.AddSingleton<ILzwDecompression, LzwDecompression>();
            
            services.AddSingleton<SharedImageParser>();
            services.AddSingleton<ILegacyParser, LegacySimpleImageParser>();
            services.AddSingleton<ILegacyParser, LegacyCrimeParser>();
            services.AddSingleton<ILegacyParser, LegacyTextParser>();
            services.AddSingleton<ILegacyParser, LegacyClueParser>();
            services.AddSingleton<ILegacyParser, LegacyPlotParser>();
            services.AddSingleton<ILegacyParser, LegacyWorldParser>();
            services.AddSingleton<ILegacyParser, LegacyCatalogParser>();
            services.AddSingleton<ILegacyParser, LegacyAnimationParser>();
            services.AddSingleton<ILegacyParser, LegacyFontsParser>();
            services.AddSingleton<ILegacyParser, LegacyProseParser>();
            services.AddSingleton<IList<ILegacyParser>>(sp => sp.GetServices<ILegacyParser>().ToList());

            services.AddSingleton<SharedImageImporter>();
            services.AddSingleton<IImporter, SimpleImageImporter>();
            services.AddSingleton<IImporter, CrimeImporter>();
            services.AddSingleton<IImporter, TextImporter>();
            services.AddSingleton<IImporter, ClueImporter>();
            services.AddSingleton<IImporter, PlotImporter>();
            services.AddSingleton<IImporter, WorldImporter>();
            services.AddSingleton<IImporter, CatalogImporter>();
            services.AddSingleton<IImporter, AnimationImporter>();
            services.AddSingleton<IImporter, FontsImporter>();
            services.AddSingleton<IImporter, ProseImporter>();
            services.AddSingleton<IList<IImporter>>(sp => sp.GetServices<IImporter>().ToList());
            
            services.AddSingleton<SharedImageExporter>();
            services.AddSingleton<IExporter<Dictionary<string, SimpleImageModel>>, SimpleImageExporter>();
            services.AddSingleton<IExporter<Dictionary<int, CrimeModel>>, CrimeExporter>();
            services.AddSingleton<IExporter<Dictionary<string, TextModel>>, TextExporter>();
            services.AddSingleton<IExporter<Dictionary<string, ClueModel>>, ClueExporter>();
            services.AddSingleton<IExporter<Dictionary<string, PlotModel>>, PlotExporter>();
            services.AddSingleton<IExporter<Dictionary<int, WorldModel>>, WorldExporter>();
            services.AddSingleton<IExporter<Dictionary<string, CatalogModel>>, CatalogExporter>();
            services.AddSingleton<IExporter<Dictionary<string, AnimationModel>>, AnimationExporter>();
            services.AddSingleton<IExporter<FontsModel>, FontsExporter>();
            services.AddSingleton<IExporter<Dictionary<string, ProseModel>>, ProseExporter>();
            
            services.AddTransient<IPackageImporter<ILegacyParser>, PackageImporter<ILegacyParser>>();
            services.AddTransient<IPackageImporter<IImporter>, PackageImporter<IImporter>>();
            services.AddTransient<IPackageExporter, PackageExporter>();

            services.AddSingleton<ICrimeTimelineProcessor, CrimeTimelineProcessor>();
            services.AddSingleton<IAnimationProcessor, AnimationProcessor>();
        }
    }
}