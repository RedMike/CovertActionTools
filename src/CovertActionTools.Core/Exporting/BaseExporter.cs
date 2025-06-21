using System;
using CovertActionTools.Core.Exporting;
using CovertActionTools.Core.Models;

namespace CovertActionTools.Core.Importing
{
    public interface IExporter
    {
        /// <summary>
        /// Returns true when process is finished
        /// </summary>
        /// <returns></returns>
        bool RunStep();
        (int current, int total) GetItemCount();
        string GetMessage();
        void Start(string path, PackageModel model);
    }

    public interface ILegacyPublisher : IExporter
    {
        
    }

    public abstract class BaseExporter<TData>: IExporter
    {
        private bool _exporting = false;
        private bool _done = false;
        private int _totalItems = 0;
        private int _currentItem = 0;
        protected string Path = string.Empty;
        protected TData Data = default!;
        
        /// <summary>
        /// Should be static
        /// </summary>
        protected abstract string Message { get; }

        protected abstract TData GetFromModel(PackageModel model);

        public bool RunStep()
        {
            if (!_exporting)
            {
                throw new Exception("Export not started");
            }

            try
            {
                var currentItem = RunExportStepInternal();
                _currentItem = currentItem;
                _done = _currentItem >= _totalItems - 1;
            }
            catch (Exception)
            {
                _currentItem++;
                throw;
            }

            if (_done)
            {
                _exporting = false;
                _currentItem = 0;
                _totalItems = 0;
                Data = default!;
                Reset();
            }
            return _done;
        }

        public (int current, int total) GetItemCount()
        {
            return (_currentItem, _totalItems);
        }

        public string GetMessage()
        {
            return Message;
        }

        public void Start(string path, PackageModel model)
        {
            if (_exporting)
            {
                throw new Exception("Already exporting");
            }
            Path = path;
            Data = GetFromModel(model);
            _exporting = true;
            OnExportStart();
            _totalItems = GetTotalItemCountInPath();
            _currentItem = 0;
        }

        protected abstract void Reset();

        /// <summary>
        /// Get the total number of items that the path will process.
        /// </summary>
        /// <returns></returns>
        protected abstract int GetTotalItemCountInPath();
        /// <summary>
        /// Returns the item index that got processed; when it matches the total number, import will be over.
        /// </summary>
        /// <returns></returns>
        protected abstract int RunExportStepInternal();
        /// <summary>
        /// Load index/etc
        /// </summary>
        protected abstract void OnExportStart();
    }
}