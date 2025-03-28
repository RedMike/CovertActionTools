using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core
{
    public static class ServiceCollectionExtension
    {
        public static void AddCovertActionsTools(this IServiceCollection services)
        {
            services.AddSingleton<IImporterFactory, ImporterFactory>();
            
            services.AddTransient<IImporter, FolderImporter>();
        }
    }
}