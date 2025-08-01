namespace AbyssCLI.Cache
{
    internal class CachedResource : IDisposable
    {
        static public CachedResource DefaultFailedResource
        {
            get
            {
                return default;
            }
        }
        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                //TODO

                _disposed = true;
            }
        }
        ~CachedResource()
        {
            Dispose(disposing: false);
        }
    }
}
