using System;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core.Services
{
    public interface IExporterFactory
    {
        IPackageExporter Create();
    }
    
    internal class ExporterFactory : IExporterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ExporterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IPackageExporter Create()
        {
            return _serviceProvider.GetRequiredService<IPackageExporter>();
        }
    }
}