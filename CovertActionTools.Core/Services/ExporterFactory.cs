using System;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Importing;
using Microsoft.Extensions.DependencyInjection;

namespace CovertActionTools.Core.Services
{
    public interface IExporterFactory
    {
        IExporter Create();
    }
    
    internal class ExporterFactory : IExporterFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ExporterFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IExporter Create()
        {
            return _serviceProvider.GetRequiredService<IExporter>();
        }
    }
}