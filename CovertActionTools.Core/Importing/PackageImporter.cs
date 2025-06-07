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

        private Task<PackageModel?>? _importTask = null;
        private ImportStatus.ImportStage _currentStage = ImportStatus.ImportStage.Unknown;
        private string _currentMessage = string.Empty;
        private int _currentTotal = 0;
        private int _currentCount = 0;

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

            foreach (var importer in _importers)
            {
                importer.Start(path);
                _logger.LogInformation($"Importer {importer.GetType()} starting import from: {path}");
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

            return new ImportStatus()
            {
                Errors = errors,
                Stage = _currentStage,
                StageMessage = _currentMessage,
                StageItems = _currentTotal,
                StageItemsDone = _currentCount,
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
                _currentStage = ImportStatus.ImportStage.ReadingIndex;
                _errors = new List<string>();
                //_logger.LogInformation($"Index: {_simpleImagesToRead.Count} images, {_crimesToRead.Count} crimes, ...");
                await Task.Yield();

                foreach (var importer in _importers)
                {
                    _currentStage = importer.GetStage();
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
                }

                await Task.Yield();
                
                _logger.LogInformation($"Import done"); //TODO: extra info
            }
            catch (Exception e)
            {
                _logger.LogError($"Exception while processing import: {e}");
                _currentStage = ImportStatus.ImportStage.Unknown;
                _currentMessage = "Error!";
                _currentTotal = 0;
                _currentCount = 0;
                throw; //we don't want to finish normally
            }
            finally
            {
                _currentStage = ImportStatus.ImportStage.ImportDone;
                _currentMessage = "Done!";
                _currentTotal = 0;
                _currentCount = 0;
            }
            
            return model;
        }
    }
}