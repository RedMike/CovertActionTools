﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CovertActionTools.Core.Importing;
using CovertActionTools.Core.Models;
using Microsoft.Extensions.Logging;

namespace CovertActionTools.Core.Exporting.Exporters
{
    /// <summary>
    /// Given a loaded model for a Crime, returns multiple assets to save:
    ///   * CRIMEx.json file (modern + metadata)
    /// </summary>
    internal class CrimeExporter : BaseExporter<Dictionary<int, CrimeModel>>
    {
#if DEBUG
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions()
        {
            WriteIndented = true
        };
#else
        private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
#endif

        private readonly ILogger<CrimeExporter> _logger;
        
        private readonly List<int> _keys = new();
        private int _index = 0;

        public CrimeExporter(ILogger<CrimeExporter> logger)
        {
            _logger = logger;
        }

        protected override string Message => "Processing crimes..";

        protected override Dictionary<int, CrimeModel> GetFromModel(PackageModel model)
        {
            return model.Crimes;
        }

        protected override void Reset()
        {
            _keys.Clear();
            _index = 0;
        }
        protected override int GetTotalItemCountInPath()
        {
            return _keys.Count;
        }

        protected override int RunExportStepInternal()
        {
            var nextKey = _keys[_index];

            var files = Export(Data[nextKey]);
            var path = GetPath(Path);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (var pair in files)
            {
                File.WriteAllBytes(System.IO.Path.Combine(path, pair.Key), pair.Value);
            }

            return _index++;
        }

        protected override void OnExportStart()
        {
            _keys.AddRange(GetKeys());
            _index = 0;
        }
        
        private List<int> GetKeys()
        {
            return Data.Keys.ToList();
        }

        private IDictionary<string, byte[]> Export(CrimeModel crime)
        {
            var dict = new Dictionary<string, byte[]>()
            {
                [$"CRIME{crime.Id}_crime.json"] = GetModernCrimeData(crime),
            };

            return dict;
        }

        private byte[] GetModernCrimeData(CrimeModel crime)
        {
            var json = JsonSerializer.Serialize(crime, JsonOptions);
            return Encoding.UTF8.GetBytes(json);
        }
        
        private string GetPath(string path)
        {
            return System.IO.Path.Combine(path, "crime");
        }
    }
}