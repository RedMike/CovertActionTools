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
        
        private Task? _exportTask = null;
        private ExportStatus.ExportStage _currentStage = ExportStatus.ExportStage.Unknown;
        private string _currentMessage = string.Empty;
        private int _currentTotal = 0;
        private int _currentCount = 0;
        private string _path = string.Empty;

        public void StartExport(PackageModel model, string path)
        {
            if (_exportTask != null && !_exportTask.IsCompleted)
            {
                throw new Exception("Trying to export when already exporting");
            }

            _path = path;
            foreach (var exporter in _exporters)
            {
                exporter.Start(path, model);
                _logger.LogInformation($"Exporter {exporter.GetType()} starting export to: {path}");
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
                        Stage = ExportStatus.ExportStage.FatalError,
                        StageMessage = _exportTask.Exception!.InnerException!.Message
                    };
                }

                return new ExportStatus()
                {
                    Errors = errors,
                    Stage = ExportStatus.ExportStage.ExportDone,
                    StageMessage = "Done!"
                };
            }
 
            return new ExportStatus()
            {
                Errors = errors,
                Stage = _currentStage,
                StageMessage = _currentMessage,
                StageItems = _currentTotal,
                StageItemsDone = _currentCount
            };
        }

        private async Task ExportInternal()
        {
            try
            {
                _currentStage = ExportStatus.ExportStage.Preparing;
                Directory.CreateDirectory(_path);
                _errors = new List<string>();
                //_logger.LogInformation($"Index: {_simpleImagesToWrite.Count} images, {_crimesToWrite.Count} crimes, ...");
                await Task.Yield();

                foreach (var exporter in _exporters)
                {
                    _currentStage = exporter.GetStage();
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
                }
                
                _currentStage = ExportStatus.ExportStage.ExportDone;
                await Task.Yield();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while processing export: {e}");
                _currentStage = ExportStatus.ExportStage.Unknown;
                _currentMessage = "Error!";
                _currentTotal = 0;
                _currentCount = 0;
                throw; //we don't want to finish normally
            }
            finally
            {
                _currentStage = ExportStatus.ExportStage.ExportDone;
                _currentMessage = "Done!";
                _currentTotal = 0;
                _currentCount = 0;
            }
            
            await Task.Yield();
            
            _logger.LogInformation($"Export done"); //TODO: extra info
        }
    }
}