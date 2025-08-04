namespace AbyssCLI.Cache
{
    public class CachedResource(HttpResponseMessage http_response) : IDisposable
    {
        protected HttpResponseMessage _http_response = http_response;
        public string MIMEType => _http_response.Content.Headers.ContentType?.MediaType ?? "";
        
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
                if (disposing)
                {
                    _http_response.Dispose();
                }
                _disposed = true;
            }
        }
        ~CachedResource()
        {
            Dispose(disposing: false);
        }
        static public CachedResource DefaultFailedResource => default;
    }
}
