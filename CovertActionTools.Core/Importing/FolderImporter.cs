using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing
{
    public interface IImporter
    {
        bool CheckIfValidForImport(string path);
        void StartImport(string path);
        ImportStatus? CheckStatus();
        PackageModel GetImportedModel();
    }
    
    internal class FolderImporter : IImporter
    {
        private readonly ILogger<FolderImporter> _logger;
        
        public FolderImporter(ILogger<FolderImporter> logger)
        {
            _logger = logger;
        }
        
        private bool _importing = false; //only one import at a time
        private string _sourcePath = string.Empty;
        private ImportStatus.ImportStage _currentStage = ImportStatus.ImportStage.Unknown;
        private int _currentItemsCount = 0;
        private int _currentItemsDoneCount = 0;

        private List<string> _errors = new List<string>();
        private List<string> _simpleImagesToRead = new List<string>();
        private List<string> _simpleImagesRead = new List<string>();

        private Task<PackageModel?>? _importTask = null;

        public bool CheckIfValidForImport(string path)
        {
            if (_importing)
            {
                return false;
            }

            //TODO: check for file existence
            
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
                _simpleImagesToRead = Directory.GetFiles(_sourcePath, "*.PIC").ToList();
                _simpleImagesRead = new List<string>();
                _logger.LogInformation($"Index: {_simpleImagesToRead.Count} images, ...");
                await Task.Yield();

                _currentStage = ImportStatus.ImportStage.ProcessingSimpleImages;
                _currentItemsCount = _simpleImagesToRead.Count;
                await Task.Yield();
                foreach (var path in _simpleImagesToRead)
                {
                    try
                    {
                        var rawData = File.ReadAllBytes(path);
                        _logger.LogInformation($"Read image: {path} {rawData.Length} bytes");

                        //import the actual image
                        //TODO:
                        var imageModel = new SimpleImageModel();

                        //update read list
                        //overwrite the entire list to keep it thread-safe
                        var newReadList = _simpleImagesRead.ToList();
                        newReadList.Add(path);
                        _simpleImagesRead = newReadList;
                        _currentItemsDoneCount = newReadList.Count;

                        //save to model
                        model.SimpleImages[Path.GetFileNameWithoutExtension(path)] = imageModel;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Error processing image: {path} {e}");
                        var newErrors = _errors.ToList();
                        newErrors.Add($"Image {path}: {e}");
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

            _logger.LogInformation($"Import done: {model.SimpleImages.Count} images, ...");
            return model;
        }
    }
}