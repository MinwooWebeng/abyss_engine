using Microsoft.ClearScript.JavaScript;

namespace AbyssCLI.Aml.API
{
    public class WebFetchApi(ResourceLoader resource_loader)
    {
        private readonly ResourceLoader _resource_loader = resource_loader;

        // Fetch the content from a URL
        public async Task<Response> FetchAsync(string resource)
        {
            Client.Client.Cerr.WriteLine("fetching");
            return await FetchAsync(resource, new RequestInit
            {
                method = "GET",
                body = null,
            });
        }
        public async Task<Response> FetchAsync(string resource, dynamic _options)
        {
            RequestInit options;
            options = new RequestInit
            {
                method = _options.method,
                body = _options.body,
            };
            Client.Client.Cerr.WriteLine("fetching " + options.method);
            return options.method switch
            {
                "GET" => new Response(await _resource_loader.TryHttpGetAsync(resource)),
                "POST" => new Response(await _resource_loader.TryHttpPostAsync(resource, options.body switch
                {
                    string text => new StringContent(text),
                    object o => new StringContent(o.ToString()),
                    _ => null
                })),
                _ => throw new Exception("not supported request method"),
            };
        }
    }
    public class RequestInit
    {
        public string method;
        public object body;
    }
    public class Response
    {
#pragma warning disable IDE1006 //naming convention
        public Response(HttpResponseMessage native_response)
        {
            ok = native_response.IsSuccessStatusCode;
            status = (int)native_response.StatusCode;
            statusText = native_response.StatusCode.ToString();

            _native_response = native_response;
        }
        public Headers headers;
        public bool ok;
        public int status;
        public string statusText;

        private readonly HttpResponseMessage _native_response;
        public object text()
        {
             return JavaScriptExtensions.ToPromise(_native_response.Content.ReadAsStringAsync());
        }
#pragma warning restore IDE1006
    }
    public class Headers
    {

    }
}
