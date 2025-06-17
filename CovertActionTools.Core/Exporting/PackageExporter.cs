using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting
{
    internal class PackageExporter<TExporter> : IPackageExporter<TExporter>
        where TExporter : IExporter
    {
        private readonly ILogger<PackageExporter<TExporter>> _logger;
        private readonly IReadOnlyList<TExporter> _exporters;
        
        public PackageExporter(ILogger<PackageExporter<TExporter>> logger, IList<TExporter> exporters)
        {
            _logger = logger;
            _exporters = exporters.ToList();
        }
        
        private List<string> _errors = new List<string>();
        
        private int _stageCount = 0;
        private int _currentStage = 0;
        private Task? _exportTask = null;
        private string _currentMessage = string.Empty;
        private int _currentTotal = 0;
        private int _currentCount = 0;
        private bool _done = false;
        private string _path = string.Empty;

        public void StartExport(PackageModel model, string path)
        {
            if (_exportTask != null && !_exportTask.IsCompleted)
            {
                throw new Exception("Trying to export when already exporting");
            }

            _path = path;
            _stageCount = 0;
            _currentStage = 0;
            _exportTask = null;
            _currentMessage = string.Empty;
            _currentTotal = 0;
            _currentCount = 0;
            _done = false;
            foreach (var exporter in _exporters)
            {
                _stageCount += 1;
                exporter.Start(path, model);
                _logger.LogDebug($"Exporter {exporter.GetType()} starting export to: {path}");
            }
            _exportTask = ExportInternal();
        }

        public ExportStatus? CheckStatus()
        {
            if (_exportTask == null)
            {
                return null;
            }

            var errors = _errors.ToList();
            if (_exportTask.IsCompleted)
            {
                if (_exportTask.IsFaulted)
                {
                    errors.Add(_exportTask.Exception!.ToString());
                    return new ExportStatus()
                    {
                        Errors = errors,
                        StageMessage = _exportTask.Exception!.InnerException!.Message,
                        StageCount = _stageCount,
                        StagesDone = _currentStage,
                        Done = _done,
                    };
                }

                return new ExportStatus()
                {
                    Errors = errors,
                    StageMessage = "Done!",
                    StageCount = _stageCount,
                    StagesDone = _currentStage,
                    Done = _done,
                };
            }
 
            return new ExportStatus()
            {
                Errors = errors,
                StageMessage = _currentMessage,
                StageItems = _currentTotal,
                StageItemsDone = _currentCount,
                StageCount = _stageCount,
                StagesDone = _currentStage,
                Done = _done,
            };
        }

        private async Task ExportInternal()
        {
            try
            {
                Directory.CreateDirectory(_path);
                _errors = new List<string>();
                await Task.Yield();

                foreach (var exporter in _exporters)
                {
                    var done = false;
                    do
                    {
                        _currentMessage = exporter.GetMessage();
                        (_currentCount, _currentTotal) = exporter.GetItemCount();
                        await Task.Yield();
                        try
                        {
                            done |= exporter.RunStep();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Exception while running step: {e}");
                            _errors.Add(e.ToString());
                            done = true;
                        }
                    } while (!done);
                    
                    _currentStage += 1;
                }
                
                await Task.Yield();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while processing export: {e}");
                _currentMessage = "Error!";
                _currentTotal = 0;
                _currentCount = 0;
                _done = true;
                throw; //we don't want to finish normally
            }
            finally
            {
                _currentMessage = "Done!";
                _currentTotal = 0;
                _currentCount = 0;
                _done = true;
            }
        }
    }
}