using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing
{
    internal class PackageImporter<TImporter> : IPackageImporter<TImporter>
        where TImporter : IImporter
    {
        private readonly ILogger<PackageImporter<TImporter>> _logger;
        private readonly IReadOnlyList<TImporter> _importers;
        
        private List<string> _errors = new List<string>();

        private int _stageCount = 0;
        private int _currentStage = 0;
        private Task<PackageModel?>? _importTask = null;
        private string _currentMessage = string.Empty;
        private int _currentTotal = 0;
        private int _currentCount = 0;
        private bool _done = false;

        public PackageImporter(ILogger<PackageImporter<TImporter>> logger, IList<TImporter> importers)
        {
            _logger = logger;
            _importers = importers.ToList();
        }

        public bool CheckIfValidForImport(string path)
        {
            if (_importTask != null && !_importTask.IsCompleted)
            {
                return false;
            }

            foreach (var importer in _importers)
            {
                if (!importer.CheckIfValid(path))
                {
                    _logger.LogWarning($"Importer {importer.GetType()} determined path is not valid: {path}");
                    return false;
                }
            }

            return true;
        }

        public void StartImport(string path)
        {
            if (_importTask != null && !_importTask.IsCompleted)
            {
                throw new Exception("Trying to import when already importing");
            }

            _stageCount = 0;
            _currentStage = 0;
            _importTask = null;
            _currentMessage = string.Empty;
            _currentTotal = 0;
            _currentCount = 0;
            _done = false;
            foreach (var importer in _importers)
            {
                _stageCount += 1;
                importer.Start(path);
                _logger.LogDebug($"Importer {importer.GetType()} starting import from: {path}");
            }
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
                        StageMessage = _importTask.Exception!.InnerException!.Message,
                        StageCount = _stageCount,
                        StagesDone = _currentStage,
                        Done = _done,
                    };
                }

                return new ImportStatus()
                {
                    Errors = errors,
                    StageMessage = "Done!",
                    StageCount = _stageCount,
                    StagesDone = _currentStage,
                    Done = _done,
                };
            }

            return new ImportStatus()
            {
                Errors = errors,
                StageMessage = _currentMessage,
                StageCount = _stageCount,
                StagesDone = _currentStage,
                StageItems = _currentTotal,
                StageItemsDone = _currentCount,
                Done = _done,
            };
        }

        public PackageModel GetImportedModel()
        {
            if (_importTask == null)
            {
                throw new Exception("Trying to read model when no task");
            }

            if (_importTask.IsFaulted)
            {
                throw _importTask.Exception!;
            }
            
            if (!_importTask.IsCompleted)
            {
                throw new Exception("Trying to read model when task pending");
            }

            return _importTask.Result!;
        }
        
        private async Task<PackageModel?> ImportInternal()
        {
            var model = new PackageModel();

            try
            {
                _errors = new List<string>();
                //_logger.LogInformation($"Index: {_simpleImagesToRead.Count} images, {_crimesToRead.Count} crimes, ...");
                await Task.Yield();

                foreach (var importer in _importers)
                {
                    var done = false;
                    var error = false;
                    do
                    {
                        _currentMessage = importer.GetMessage();
                        (_currentCount, _currentTotal) = importer.GetItemCount();
                        await Task.Yield();
                        try
                        {
                            done |= importer.RunStep();
                        }
                        catch (Exception e)
                        {
                            _logger.LogError($"Exception while running step: {e}");
                            _errors.Add(e.ToString());
                            done = true;
                            error = true;
                        }
                    } while (!done);

                    if (!error)
                    {
                        importer.SetResult(model);
                    }

                    _currentStage += 1;
                }

                await Task.Yield();
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while processing import: {e}");
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
            
            return model;
        }
    }
}