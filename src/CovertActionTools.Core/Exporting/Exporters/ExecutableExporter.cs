using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    internal class ExecutableExporter : BaseExporter<Dictionary<string, ExecutableModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<ExecutableExporter> _logger;

        private bool _done = false;

        public ExecutableExporter(ILogger<ExecutableExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing executables..";

        protected override Dictionary<string, ExecutableModel> GetFromModel(PackageModel model)
        {
            return model.Executables;
        }

        protected override void Reset()
        {
            _done = false;
        }

        protected override int GetTotalItemCountInPath()
        {
            return Data.Count > 0 ? 1 : 0;
        }

        protected override int RunExportStepInternal()
        {
            if (Data.Count == 0 || _done)
            {
                return 1;
            }

            var json = JsonSerializer.Serialize(Data, JsonOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            File.WriteAllBytes(System.IO.Path.Combine(Path, "EXECUTABLES.json"), bytes);

            _done = true;
            return 1;
        }

        protected override void OnExportStart()
        {
            _done = false;
        }
    }
}
