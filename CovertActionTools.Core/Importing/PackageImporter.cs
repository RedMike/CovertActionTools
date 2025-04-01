using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Importing.Importers;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing
{
    internal class PackageImporter : IImporter
    {
        private readonly ILogger<PackageImporter> _logger;
        private readonly ISimpleImageImporter _simpleImageImporter;
        private readonly ICrimeImporter _crimeImporter;
        
        private bool _importing = false; //only one import at a time
        private string _sourcePath = string.Empty;
        private ImportStatus.ImportStage _currentStage = ImportStatus.ImportStage.Unknown;
        private int _currentItemsCount = 0;
        private int _currentItemsDoneCount = 0;

        private List<string> _errors = new List<string>();
        private List<string> _simpleImagesToRead = new List<string>();
        private List<string> _simpleImagesRead = new List<string>();
        private List<string> _crimesToRead = new List<string>();
        private List<string> _crimesRead = new List<string>();

        private Task<PackageModel?>? _importTask = null;

        public PackageImporter(ILogger<PackageImporter> logger, ISimpleImageImporter simpleImageImporter, ICrimeImporter crimeImporter)
        {
            _logger = logger;
            _simpleImageImporter = simpleImageImporter;
            _crimeImporter = crimeImporter;
        }

        public bool CheckIfValidForImport(string path)
        {
            if (_importing)
            {
                return false;
            }

            //must have at least one json file
            //TODO: better check?
            var legacyFiles = Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly);
            if (legacyFiles.Length == 0)
            {
                return false;
            }
            
            return true;
        }

        public void StartImport(string path)
        {
            if (_importing)
            {
                throw new Exception("Trying to import when already importing");
            }

            _importing = true;
            _sourcePath = path;
            _currentStage = ImportStatus.ImportStage.ReadingIndex;
            _logger.LogInformation($"Starting import from: {path}");
            _importTask = ImportInternal();
        }

        public ImportStatus? CheckStatus()
        {
            if (_importTask == null)
            {
                return null;
            }

            var errors = _errors.ToList();
            if (_importTask.IsCompleted)
            {
                if (_importTask.IsFaulted)
                {
                    errors.Add(_importTask.Exception!.ToString());
                    return new ImportStatus()
                    {
                        Errors = errors,
                        Stage = ImportStatus.ImportStage.FatalError,
                        StageMessage = _importTask.Exception!.InnerException!.Message
                    };
                }

                return new ImportStatus()
                {
                    Errors = errors,
                    Stage = ImportStatus.ImportStage.ImportDone,
                    StageMessage = "Done!"
                };
            }

            var msg = "Unknown.";
            switch (_currentStage)
            {
                case ImportStatus.ImportStage.ReadingIndex:
                    msg = "Reading index..";
                    break;
                case ImportStatus.ImportStage.ProcessingSimpleImages:
                    msg = $"Processing simple images ({_simpleImagesRead.Count}/{_simpleImagesToRead.Count})";
                    break;
                case ImportStatus.ImportStage.ProcessingCrimes:
                    msg = $"Processing crimes ({_crimesRead.Count}/{_crimesToRead.Count})";
                    break;
                case ImportStatus.ImportStage.ImportDone:
                    msg = $"Done";
                    break;
            }
            
            return new ImportStatus()
            {
                Errors = errors,
                Stage = _currentStage,
                StageMessage = msg,
                StageItems = _currentItemsCount,
                StageItemsDone = _currentItemsDoneCount,
            };
        }

        public PackageModel GetImportedModel()
        {
            if (_importTask == null)
            {
                throw new Exception("Trying to read model when no task");
            }

            if (!_importTask.IsCompleted)
            {
                throw new Exception("Trying to read model when task pending");
            }

            if (_importTask.IsFaulted)
            {
                throw _importTask.Exception!;
            }

            return _importTask.Result!;
        }
        
        private async Task<PackageModel?> ImportInternal()
        {
            var model = new PackageModel();

            try
            {
                _currentStage = ImportStatus.ImportStage.ReadingIndex;
                _currentItemsCount = 0;
                _currentItemsDoneCount = 0;
                _errors = new List<string>();
                _simpleImagesToRead = Directory.GetFiles(_sourcePath, "*_image.json")
                    .OrderBy(x => x)
                    .ToList();
                _simpleImagesRead = new List<string>();
                _crimesToRead = Directory.GetFiles(_sourcePath, "*_crime.json")
                    .OrderBy(x => x)
                    .ToList();
                _crimesRead = new List<string>();
                _logger.LogInformation($"Index: {_simpleImagesToRead.Count} images, {_crimesToRead.Count} crimes, ...");
                await Task.Yield();

                _currentStage = ImportStatus.ImportStage.ProcessingSimpleImages;
                _currentItemsCount = _simpleImagesToRead.Count;
                await Task.Yield();
                foreach (var path in _simpleImagesToRead)
                {
                    var fileName = Path.GetFileNameWithoutExtension(path).Replace("_image", "");
                    try
                    {
                        var imageModel = _simpleImageImporter.Import(_sourcePath, fileName);
                        _logger.LogInformation($"Read image: {fileName}");

                        //update read list
                        //overwrite the entire list to keep it thread-safe
                        var newReadList = _simpleImagesRead.ToList();
                        newReadList.Add(path);
                        _simpleImagesRead = newReadList;
                        _currentItemsDoneCount = newReadList.Count;

                        //save to model
                        model.SimpleImages[fileName] = imageModel;
                    }
                    catch (Exception e)
                    {
                        //individual image failures don't crash the entire import
                        _logger.LogError($"Error processing image: {fileName} {e}");
                        var newErrors = _errors.ToList();
                        newErrors.Add($"Image {fileName}: {e}");
                        _errors = newErrors;
                    }

                    await Task.Yield();
                }
                
                _currentStage = ImportStatus.ImportStage.ProcessingCrimes;
                _currentItemsCount = _crimesToRead.Count;
                await Task.Yield();
                foreach (var path in _crimesToRead)
                {
                    var fileName = Path.GetFileNameWithoutExtension(path).Replace("_crime", "");
                    try
                    {
                        var crimeModel = _crimeImporter.Import(_sourcePath, fileName);
                        _logger.LogInformation($"Read crime: {fileName}");

                        //update read list
                        //overwrite the entire list to keep it thread-safe
                        var newReadList = _crimesRead.ToList();
                        newReadList.Add(path);
                        _crimesRead = newReadList;
                        _currentItemsDoneCount = newReadList.Count;

                        //save to model
                        model.Crimes[fileName] = crimeModel;
                    }
                    catch (Exception e)
                    {
                        //individual image failures don't crash the entire import
                        _logger.LogError($"Error processing crime: {fileName} {e}");
                        var newErrors = _errors.ToList();
                        newErrors.Add($"Crime {fileName}: {e}");
                        _errors = newErrors;
                    }

                    await Task.Yield();
                }

                _currentStage = ImportStatus.ImportStage.ImportDone;
                await Task.Yield();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while processing import: {e}");
                throw; //we don't want to finish normally
            }
            finally
            {
                _currentStage = ImportStatus.ImportStage.ImportDone;
            }

            _logger.LogInformation($"Import done: {model.SimpleImages.Count} images, {model.Crimes.Count}, ...");
            return model;
        }
    }
}