using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Exporting.Exporters;
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
        private readonly ISimpleImageExporter _simpleImageExporter;
        private readonly ICrimeExporter _crimeExporter;
        private readonly ITextExporter _textExporter;

        public PackageExporter(ILogger<PackageExporter> logger, ISimpleImageExporter simpleImageExporter, ICrimeExporter crimeExporter, ITextExporter textExporter)
        {
            _logger = logger;
            _simpleImageExporter = simpleImageExporter;
            _crimeExporter = crimeExporter;
            _textExporter = textExporter;
        }
        
        private bool _exporting = false; //only one export at a time
        private PackageModel? _package = null;
        private string _destinationPath = string.Empty;
        private ExportStatus.ExportStage _currentStage = ExportStatus.ExportStage.Unknown;
        private int _currentItemsCount = 0;
        private int _currentItemsDoneCount = 0;

        private List<string> _errors = new List<string>();
        private List<string> _simpleImagesToWrite = new List<string>();
        private List<string> _simpleImagesWritten = new List<string>();
        private List<int> _crimesToWrite = new List<int>();
        private List<int> _crimesWritten = new List<int>();
        private string? _textToWrite = null;
        private string? _textWritten = null;

        private Task? _exportTask = null;

        public void StartExport(PackageModel model, string path)
        {
            if (_exporting)
            {
                throw new Exception("Trying to export when already exporting");
            }

            _exporting = true;
            _destinationPath = path;
            _package = model;
            _currentStage = ExportStatus.ExportStage.Preparing;
            _logger.LogInformation($"Starting export to: {path}");
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

            var msg = "Unknown.";
            switch (_currentStage)
            {
                case ExportStatus.ExportStage.Preparing:
                    msg = "Preparing..";
                    break;
                case ExportStatus.ExportStage.ProcessingSimpleImages:
                    msg = $"Processing simple images ({_simpleImagesWritten.Count}/{_simpleImagesToWrite.Count})";
                    break;
                case ExportStatus.ExportStage.ProcessingCrimes:
                    msg = $"Processing crimes ({_crimesWritten.Count}/{_crimesToWrite.Count})";
                    break;
                case ExportStatus.ExportStage.ProcessingTexts:
                    msg = $"Processing texts..";
                    break;
                case ExportStatus.ExportStage.ExportDone:
                    msg = "Done";
                    break;
            }

            return new ExportStatus()
            {
                Errors = errors,
                Stage = _currentStage,
                StageMessage = msg,
                StageItems = _currentItemsCount,
                StageItemsDone = _currentItemsDoneCount
            };
        }

        private async Task ExportInternal()
        {
            if (_package == null)
            {
                throw new Exception("Missing package");
            }
            
            try
            {
                _currentStage = ExportStatus.ExportStage.Preparing;
                Directory.CreateDirectory(_destinationPath);
                _currentItemsCount = 0;
                _currentItemsDoneCount = 0;
                _errors = new List<string>();

                _simpleImagesToWrite = _package.SimpleImages.Keys.OrderBy(x => x).ToList();
                _simpleImagesWritten = new List<string>();

                _crimesToWrite = _package.Crimes.Keys.OrderBy(x => x).ToList();
                _crimesWritten = new List<int>();

                _textToWrite = "text";
                _textToWrite = null;
                
                _logger.LogInformation($"Index: {_simpleImagesToWrite.Count} images, {_crimesToWrite.Count} crimes, ...");
                await Task.Yield();

                _currentStage = ExportStatus.ExportStage.ProcessingSimpleImages;
                _currentItemsCount = _simpleImagesToWrite.Count;
                await Task.Yield();
                foreach (var imageKey in _simpleImagesToWrite)
                {
                    var image = _package.SimpleImages[imageKey];
                    try
                    {
                        var files = _simpleImageExporter.Export(image);
                        foreach (var pair in files)
                        {
                            var (fileName, bytes) = (pair.Key, pair.Value);
                            File.WriteAllBytes(Path.Combine(_destinationPath, fileName), bytes);
                            _logger.LogInformation($"Wrote image: {fileName} {bytes.Length}");
                        }
                    }
                    catch (Exception e)
                    {
                        //individual image failures don't crash the entire export
                        _logger.LogError($"Error processing image: {imageKey} {e}");
                        var newErrors = _errors.ToList();
                        newErrors.Add($"Image {imageKey}: {e}");
                        _errors = newErrors;
                    }
                }
                
                
                _currentStage = ExportStatus.ExportStage.ProcessingCrimes;
                _currentItemsCount = _crimesToWrite.Count;
                await Task.Yield();
                foreach (var crimeKey in _crimesToWrite)
                {
                    var crime = _package.Crimes[crimeKey];
                    try
                    {
                        var files = _crimeExporter.Export(crime);
                        foreach (var pair in files)
                        {
                            var (fileName, bytes) = (pair.Key, pair.Value);
                            File.WriteAllBytes(Path.Combine(_destinationPath, fileName), bytes);
                            _logger.LogInformation($"Wrote crime: {fileName} {bytes.Length}");
                        }
                    }
                    catch (Exception e)
                    {
                        //individual crime failures don't crash the entire export
                        _logger.LogError($"Error processing crime: {crimeKey} {e}");
                        var newErrors = _errors.ToList();
                        newErrors.Add($"Crime {crimeKey}: {e}");
                        _errors = newErrors;
                    }
                }
                
                
                _currentStage = ExportStatus.ExportStage.ProcessingTexts;
                _currentItemsCount = 1;
                await Task.Yield();
                var text = _package.Texts;
                try
                {
                    var files = _textExporter.Export(text);
                    foreach (var pair in files)
                    {
                        var (fileName, bytes) = (pair.Key, pair.Value);
                        File.WriteAllBytes(Path.Combine(_destinationPath, fileName), bytes);
                        _logger.LogInformation($"Wrote text: {fileName} {bytes.Length}");
                    }
                }
                catch (Exception e)
                {
                    //individual text failures don't crash the entire export
                    _logger.LogError($"Error processing text: {e}");
                    var newErrors = _errors.ToList();
                    newErrors.Add($"Text: {e}");
                    _errors = newErrors;
                }
                
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
            
            _logger.LogInformation($"Export done: {_package.SimpleImages.Count} images, {_package.Crimes.Count} crimes, ...");
        }
    }
}