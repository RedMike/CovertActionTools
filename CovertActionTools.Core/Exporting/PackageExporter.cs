using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Exporting.Exporters;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting
{
    public interface IPackageExporter
    {
        void StartExport(PackageModel model, string path);
        ExportStatus? CheckStatus();
    }
    
    internal class PackageExporter : IPackageExporter
    {
        private readonly ILogger<PackageExporter> _logger;
        private readonly IExporter<Dictionary<string, SimpleImageModel>> _simpleImageExporter;
        private readonly IExporter<Dictionary<int, CrimeModel>> _crimeExporter;
        private readonly IExporter<Dictionary<string, TextModel>> _textExporter;

        public PackageExporter(ILogger<PackageExporter> logger, IExporter<Dictionary<string, SimpleImageModel>> simpleImageExporter, IExporter<Dictionary<int, CrimeModel>> crimeExporter, IExporter<Dictionary<string, TextModel>> textExporter)
        {
            _logger = logger;
            _simpleImageExporter = simpleImageExporter;
            _crimeExporter = crimeExporter;
            _textExporter = textExporter;
        }
        
        private List<string> _errors = new List<string>();
        
        private Task? _exportTask = null;
        private ExportStatus.ExportStage _currentStage = ExportStatus.ExportStage.Unknown;
        private IExporter? _currentExporter = null;
        private string _path = string.Empty;

        public void StartExport(PackageModel model, string path)
        {
            if (_exportTask != null && !_exportTask.IsCompleted)
            {
                throw new Exception("Trying to export when already exporting");
            }

            _path = path;
            _simpleImageExporter.Start(path, model.SimpleImages);
            _logger.LogInformation($"Exporter {_simpleImageExporter.GetType()} starting export to: {path}");
            _crimeExporter.Start(path, model.Crimes);
            _logger.LogInformation($"Exporter {_crimeExporter.GetType()} starting export to: {path}");
            _textExporter.Start(path, model.Texts);
            _logger.LogInformation($"Exporter {_textExporter.GetType()} starting export to: {path}");
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

            if (_currentExporter == null)
            {
                throw new Exception("Missing exporter");
            }

            var (current, total) = _currentExporter.GetItemCount(); 
            return new ExportStatus()
            {
                Errors = errors,
                Stage = _currentStage,
                StageMessage = _currentExporter.GetMessage(),
                StageItems = total,
                StageItemsDone = current
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

                //images
                _currentStage = ExportStatus.ExportStage.ProcessingSimpleImages;
                _currentExporter = _simpleImageExporter;
                await Task.Yield();
                var done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _simpleImageExporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                
                //crimes
                _currentStage = ExportStatus.ExportStage.ProcessingCrimes;
                _currentExporter = _simpleImageExporter;
                await Task.Yield();
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _crimeExporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                
                //texts
                _currentStage = ExportStatus.ExportStage.ProcessingTexts;
                _currentExporter = _simpleImageExporter;
                await Task.Yield();
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _textExporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                
                await Task.Yield();
                _currentStage = ExportStatus.ExportStage.ExportDone;
                await Task.Yield();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while processing export: {e}");
                throw; //we don't want to finish normally
            }
            finally
            {
                _currentStage = ExportStatus.ExportStage.ExportDone;
            }
            
            //_logger.LogInformation($"Export done: {.SimpleImages.Count} images, {_package.Crimes.Count} crimes, ...");
        }
    }
}