using System;

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
    }

    public interface IExporter<in TData> : IExporter
    {
        void Start(string path, string? publishPath, TData data);
    }

    public abstract class BaseExporter<TData>: IExporter<TData>
    {
        private bool _exporting = false;
        private bool _done = false;
        private int _totalItems = 0;
        private int _currentItem = 0;
        protected string Path = string.Empty;
        protected string? PublishPath = null;
        protected TData Data = default!;
        
        /// <summary>
        /// Should be static
        /// </summary>
        protected abstract string Message { get; }

        public void Start(string path, string? publishPath, TData data)
        {
            if (_exporting)
            {
                throw new Exception("Already exporting");
            }
            Path = path;
            PublishPath = publishPath;
            Data = data;
            _exporting = true;
            OnExportStart();
            _totalItems = GetTotalItemCountInPath();
            _currentItem = 0;
        }

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