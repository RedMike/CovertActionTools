using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Importing
{
    internal class PackageImporter : IPackageImporter
    {
        private readonly ILogger<PackageImporter> _logger;
        private readonly IReadOnlyList<IImporter> _importers;
        private readonly IImporter<Dictionary<string, SimpleImageModel>> _simpleImageImporter;
        private readonly IImporter<Dictionary<int, CrimeModel>> _crimeImporter;
        private readonly IImporter<Dictionary<string, TextModel>> _textImporter;
        private readonly IImporter<Dictionary<string, ClueModel>> _clueImporter;
        private readonly IImporter<Dictionary<string, PlotModel>> _plotImporter;
        private readonly IImporter<Dictionary<int, WorldModel>> _worldImporter;
        private readonly IImporter<Dictionary<string, CatalogModel>> _catalogImporter;
        
        private List<string> _errors = new List<string>();

        private Task<PackageModel?>? _importTask = null;
        private ImportStatus.ImportStage _currentStage = ImportStatus.ImportStage.Unknown;
        private IImporter? _currentImporter = null;

        public PackageImporter(ILogger<PackageImporter> logger, IImporter<Dictionary<string, SimpleImageModel>> simpleImageImporter, IImporter<Dictionary<int, CrimeModel>> crimeImporter, IImporter<Dictionary<string, TextModel>> textImporter, IImporter<Dictionary<string, ClueModel>> clueImporter, IImporter<Dictionary<string, PlotModel>> plotImporter, IImporter<Dictionary<int, WorldModel>> worldImporter, IImporter<Dictionary<string, CatalogModel>> catalogImporter)
        {
            _logger = logger;
            _simpleImageImporter = simpleImageImporter;
            _crimeImporter = crimeImporter;
            _textImporter = textImporter;
            _clueImporter = clueImporter;
            _plotImporter = plotImporter;
            _worldImporter = worldImporter;
            _catalogImporter = catalogImporter;
            _importers = new IImporter[]
            {
                _simpleImageImporter,
                _crimeImporter,
                _textImporter,
                _clueImporter,
                _plotImporter,
                _worldImporter,
                _catalogImporter
            };
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

            if (_currentImporter == null)
            {
                throw new Exception("Missing importer");
            }

            var (current, total) = _currentImporter.GetItemCount(); 
            return new ImportStatus()
            {
                Errors = errors,
                Stage = _currentStage,
                StageMessage = _currentImporter.GetMessage(),
                StageItems = total,
                StageItemsDone = current,
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
                _errors = new List<string>();
                //_logger.LogInformation($"Index: {_simpleImagesToRead.Count} images, {_crimesToRead.Count} crimes, ...");
                await Task.Yield();

                //images
                _currentStage = ImportStatus.ImportStage.ProcessingSimpleImages;
                _currentImporter = _simpleImageImporter;
                var done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.SimpleImages = _simpleImageImporter.GetResult();
                
                //crimes
                _currentStage = ImportStatus.ImportStage.ProcessingCrimes;
                _currentImporter = _crimeImporter;
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.Crimes = _crimeImporter.GetResult();

                //texts
                _currentStage = ImportStatus.ImportStage.ProcessingTexts;
                _currentImporter = _textImporter;
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.Texts = _textImporter.GetResult();
                
                //clues
                _currentStage = ImportStatus.ImportStage.ProcessingClues;
                _currentImporter = _clueImporter;
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.Clues = _clueImporter.GetResult();
                
                //plots
                _currentStage = ImportStatus.ImportStage.ProcessingPlots;
                _currentImporter = _plotImporter;
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.Plots = _plotImporter.GetResult();
                
                //worlds
                _currentStage = ImportStatus.ImportStage.ProcessingWorlds;
                _currentImporter = _worldImporter;
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.Worlds = _worldImporter.GetResult();
                
                //catalogs
                _currentStage = ImportStatus.ImportStage.ProcessingCatalogs;
                _currentImporter = _catalogImporter;
                done = false;
                do
                {
                    await Task.Yield();
                    try
                    {
                        done |= _currentImporter.RunStep();
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Exception while running step: {e}");
                        _errors.Add(e.ToString());
                    }
                } while (!done);
                model.Catalogs = _catalogImporter.GetResult();

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

            _logger.LogInformation($"Import done: {model.SimpleImages.Count} images, " +
                                   $"{model.Crimes.Count} crimes, " +
                                   $"{model.Texts.Count} texts, " +
                                   $"{model.Clues.Count} clues, " +
                                   $"{model.Plots.Count} plots, " +
                                   $"{model.Worlds.Count} worlds, " +
                                   $"{model.Catalogs.Count} catalogs, " +
                                   $"...");
            return model;
        }
    }
}