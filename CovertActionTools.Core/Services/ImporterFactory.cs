using System;
using CovertActionTools.Core.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core.Services
{
    public interface IImporterFactory
    {
        IPackageImporter Create(bool legacy);
    }
    
    internal class ImporterFactory : IImporterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ImporterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPackageImporter Create(bool legacy)
        {
            if (legacy)
            {
                return _serviceProvider.GetRequiredService<LegacyFolderImporter>();
            }

            return _serviceProvider.GetRequiredService<IPackageImporter>();
        }
    }
}