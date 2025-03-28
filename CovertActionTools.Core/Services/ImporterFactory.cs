using System;
using CovertActionTools.Core.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core.Services
{
    public interface IImporterFactory
    {
        IImporter Create();
    }
    
    internal class ImporterFactory : IImporterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ImporterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IImporter Create()
        {
            return _serviceProvider.GetRequiredService<IImporter>();
        }
    }
}