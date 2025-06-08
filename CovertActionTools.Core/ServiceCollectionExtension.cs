using System.Collections.Generic;
using System.Linq;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Exporting.Publishers;
using CovertActionTools.Core.Exporting.Shared;
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
            services.AddSingleton<IExporter, SimpleImageExporter>();
            services.AddSingleton<IExporter, CrimeExporter>();
            services.AddSingleton<IExporter, TextExporter>();
            services.AddSingleton<IExporter, ClueExporter>();
            services.AddSingleton<IExporter, PlotExporter>();
            services.AddSingleton<IExporter, WorldExporter>();
            services.AddSingleton<IExporter, CatalogExporter>();
            services.AddSingleton<IExporter, AnimationExporter>();
            services.AddSingleton<IExporter, FontsExporter>();
            services.AddSingleton<IExporter, ProseExporter>();
            services.AddSingleton<IList<IExporter>>(sp => sp.GetServices<IExporter>().ToList());

            services.AddSingleton<ILegacyPublisher, AnimationPublisher>();
            services.AddSingleton<ILegacyPublisher, CatalogPublisher>();
            services.AddSingleton<ILegacyPublisher, CluePublisher>();
            services.AddSingleton<ILegacyPublisher, CrimePublisher>();
            services.AddSingleton<ILegacyPublisher, FontsPublisher>();
            services.AddSingleton<ILegacyPublisher, PlotPublisher>();
            services.AddSingleton<ILegacyPublisher, ProsePublisher>();
            services.AddSingleton<ILegacyPublisher, SimpleImagePublisher>();
            services.AddSingleton<ILegacyPublisher, TextPublisher>();
            services.AddSingleton<ILegacyPublisher, WorldPublisher>();
            services.AddSingleton<IList<ILegacyPublisher>>(sp => sp.GetServices<ILegacyPublisher>().ToList());
            
            services.AddTransient<IPackageImporter<ILegacyParser>, PackageImporter<ILegacyParser>>();
            services.AddTransient<IPackageImporter<IImporter>, PackageImporter<IImporter>>();
            services.AddTransient<IPackageExporter<IExporter>, PackageExporter<IExporter>>();
            services.AddTransient<IPackageExporter<ILegacyPublisher>, PackageExporter<ILegacyPublisher>>();

            services.AddSingleton<ICrimeTimelineProcessor, CrimeTimelineProcessor>();
            services.AddSingleton<IAnimationProcessor, AnimationProcessor>();
        }
    }
}