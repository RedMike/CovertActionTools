using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace CovertActionTools.Core.Exporting
{
    public interface IExporter
    {
        void StartExport(PackageModel model, string path);
        ExportStatus? CheckStatus();
    }
    
    internal class PackageExporter : IExporter
    {
        private readonly ILogger<PackageExporter> _logger;

        public PackageExporter(ILogger<PackageExporter> logger)
        {
            _logger = logger;
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
                _logger.LogInformation($"Index: {_simpleImagesToWrite.Count} images, ...");
                await Task.Yield();

                _currentStage = ExportStatus.ExportStage.ProcessingSimpleImages;
                _currentItemsCount = _simpleImagesToWrite.Count;
                await Task.Yield();
                foreach (var fileName in _simpleImagesToWrite)
                {
                    var image = _package.SimpleImages[fileName];
                    var path = $"{fileName}.png";
                    try
                    {
                        var rawData = image.RawImageData;
                        var width = image.Width;
                        var height = image.Height;
                        var imageData = new byte[width * height * 4];
                        for (var i = 0; i < width; i++)
                        {
                            for (var j = 0; j < height; j++)
                            {
                                byte r = 0;
                                byte g = 0;
                                byte b = 0;
                                byte a = 255;
                                var p = rawData[j * width + i];
                                switch (p)
                                {
                                    case 0: a = 0; break;
                                    case 1: b = 0xAA; break;
                                    case 2: g = 0xAA; break;
                                    case 3: g = 0xAA; b = 0xAA; break;
                                    case 4: r = 0xAA; break;
                                    case 5: break;
                                    case 6: r = 0xAA; g = 0x55; break;
                                    case 7: r = 0xAA; g = 0xAA; b = 0xAA; break;
                                    case 8: r = 0x55; g = 0x55; b = 0x55; break;
                                    case 9: r = 0x55; g = 0x55; b = 0xFF; break;
                                    case 10: r = 0x55; g = 0xFF; b = 0x55; break;
                                    case 11: r = 0x55; g = 0xFF; b = 0xFF; break;
                                    case 12: r = 0xFF; g = 0x55; b = 0x55; break;
                                    case 13: r = 0xFF; g = 0x55; b = 0xFF; break;
                                    case 14: r = 0xFF; g = 0xFF; b = 0x55; break;
                                    case 15: r = 0xFF; g = 0xFF; b = 0xFF; break;
                                    default:
                                        throw new Exception($"Unknown pixel value: {p}");
                                }
                                imageData[(j * width + i) * 4 + 0] = r;
                                imageData[(j * width + i) * 4 + 1] = g;
                                imageData[(j * width + i) * 4 + 2] = b;
                                imageData[(j * width + i) * 4 + 3] = a;
                            }
                        }

                        // using var bitmap = SKBitmap.Decode(imageData,
                        //     new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul));
                        // using var imageObj = SKImage.FromBitmap(bitmap);
                        using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
                        IntPtr pixels = bitmap.GetPixels();
                        int buffSize = bitmap.Height * bitmap.RowBytes;
                        byte[] pixelBuffer = new byte[buffSize];
                        int q = 0;
                        int x = 0;
                        int padding = bitmap.RowBytes - (4 * width);
                        for (var row = 0; row < height; row++)
                        {
                            for (int col = 0; col < width; col++)
                            {
                                pixelBuffer[x++] = imageData[(row*width + col)*4];
                                pixelBuffer[x++] = imageData[(row*width + col)*4+1];
                                pixelBuffer[x++] = imageData[(row*width + col)*4+2];
                                pixelBuffer[x++] = imageData[(row*width + col)*4+3];
                            }

                            x += padding;
                        }

                        Marshal.Copy(pixelBuffer, 0, pixels, buffSize);
                        
                        using var imageFile = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
                        using var file = File.OpenWrite(Path.Combine(_destinationPath, path));
                        imageFile.SaveTo(file);
                        file.Flush();

                        _logger.LogInformation($"Wrote image: {fileName}");
                    }
                    catch (Exception e)
                    {
                        //individual image failures don't crash the entire export
                        _logger.LogError($"Error processing image: {path} {e}");
                        var newErrors = _errors.ToList();
                        newErrors.Add($"Image {fileName}: {e}");
                        _errors = newErrors;
                    }
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
            
            _logger.LogInformation($"Export done: {_package.SimpleImages.Count} images, ...");
        }
    }
}