using System.Collections.Generic;
using CovertActionTools.Core.Compression;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Importing.Parsers;
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
            services.AddSingleton<LegacySimpleImageParser>();
            services.AddSingleton<LegacyCrimeParser>();
            services.AddSingleton<LegacyTextParser>();
            services.AddSingleton<LegacyClueParser>();
            services.AddSingleton<LegacyPlotParser>();
            services.AddSingleton<LegacyWorldParser>();
            services.AddSingleton<LegacyCatalogParser>();
            services.AddSingleton<LegacyAnimationParser>();

            services.AddSingleton<SharedImageImporter>();
            services.AddSingleton<IImporter<Dictionary<string, SimpleImageModel>>, SimpleImageImporter>();
            services.AddSingleton<IImporter<Dictionary<int, CrimeModel>>, CrimeImporter>();
            services.AddSingleton<IImporter<Dictionary<string, TextModel>>, TextImporter>();
            services.AddSingleton<IImporter<Dictionary<string, ClueModel>>, ClueImporter>();
            services.AddSingleton<IImporter<Dictionary<string, PlotModel>>, PlotImporter>();
            services.AddSingleton<IImporter<Dictionary<int, WorldModel>>, WorldImporter>();
            services.AddSingleton<IImporter<Dictionary<string, CatalogModel>>, CatalogImporter>();
            services.AddSingleton<IImporter<Dictionary<string, AnimationModel>>, AnimationImporter>();
            
            services.AddSingleton<SharedImageExporter>();
            services.AddSingleton<IExporter<Dictionary<string, SimpleImageModel>>, SimpleImageExporter>();
            services.AddSingleton<IExporter<Dictionary<int, CrimeModel>>, CrimeExporter>();
            services.AddSingleton<IExporter<Dictionary<string, TextModel>>, TextExporter>();
            services.AddSingleton<IExporter<Dictionary<string, ClueModel>>, ClueExporter>();
            services.AddSingleton<IExporter<Dictionary<string, PlotModel>>, PlotExporter>();
            services.AddSingleton<IExporter<Dictionary<int, WorldModel>>, WorldExporter>();
            services.AddSingleton<IExporter<Dictionary<string, CatalogModel>>, CatalogExporter>();
            services.AddSingleton<IExporter<Dictionary<string, AnimationModel>>, AnimationExporter>();
            
            services.AddTransient<LegacyFolderImporter>();
            services.AddTransient<IPackageImporter, PackageImporter>();
            services.AddTransient<IPackageExporter, PackageExporter>();

            services.AddSingleton<ICrimeTimelineProcessor, CrimeTimelineProcessor>();
            services.AddSingleton<IAnimationProcessor, AnimationProcessor>();
        }
    }
}