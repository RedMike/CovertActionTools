using System;

namespace CovertActionTools.Core.Importing
{
    public interface IImporter
    {
        bool CheckIfValid(string path);
        void Start(string path);
        /// <summary>
        /// Returns true when process is finished
        /// </summary>
        /// <returns></returns>
        bool RunStep();
        (int current, int total) GetItemCount();
        string GetMessage();
    }

    public interface IImporter<out TData> : IImporter
    {
        TData GetResult();
    }
    
    internal abstract class BaseImporter<TData>: IImporter<TData>
    {
        private bool _importing = false;
        private bool _done = false;
        private int _totalItems = 0;
        private int _currentItem = 0;
        protected string Path = string.Empty;
        
        /// <summary>
        /// Should be static
        /// </summary>
        protected abstract string Message { get; }
        
        public bool CheckIfValid(string path)
        {
            if (_importing)
            {
                throw new Exception("Already importing");
            }

            return CheckIfValidForImportInternal(path);
        }

        public void Start(string path)
        {
            if (_importing)
            {
                throw new Exception("Already importing");
            }
            Path = path;
            _importing = true;
            OnImportStart();
            _totalItems = GetTotalItemCountInPath();
            _currentItem = 0;
        }

        public bool RunStep()
        {
            if (!_importing)
            {
                throw new Exception("Import not started");
            }

            try
            {
                var currentItem = RunImportStepInternal();
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

        public TData GetResult()
        {
            if (!_done)
            {
                throw new Exception("Import not finished");
            }

            return GetResultInternal();
        }

        protected abstract bool CheckIfValidForImportInternal(string path);
        /// <summary>
        /// Get the total number of items that the path will process.
        /// </summary>
        /// <returns></returns>
        protected abstract int GetTotalItemCountInPath();
        /// <summary>
        /// Returns the item index that got processed; when it matches the total number, import will be over.
        /// </summary>
        /// <returns></returns>
        protected abstract int RunImportStepInternal();
        protected abstract TData GetResultInternal();
        /// <summary>
        /// Load index/etc
        /// </summary>
        protected abstract void OnImportStart();
    }
}