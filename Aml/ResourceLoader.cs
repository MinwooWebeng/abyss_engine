using AbyssCLI.Tool;
using Google.Protobuf.WellKnownTypes;
using System.IO.MemoryMappedFiles;

namespace AbyssCLI.Aml
{
    //each content must have one MediaLoader
    //TODO: add CORS protection before adding cookie.
    public class ResourceLoader
    {
        public ResourceLoader(AbyssLib.Host host, AbyssURL origin)
        {
            if (origin.Scheme == "abyst")
            {
                var result = host.GetAbystClient(origin.Id);
                if (!result.Item2.Empty)
                {
                    Client.Client.Cerr.WriteLine("failed to get abyst client (" + origin.Raw + ") : " + result.Item2.Message);
                    IsValid = false;
                    _abyst_client = new AbyssLib.AbystClient(IntPtr.Zero);
                    return;
                }
                _abyst_client = result.Item1;
                _mmf_path_prefix = "abyst_" + origin.Id[..8] + "_";
            }
            else
            {
                _abyst_client = new AbyssLib.AbystClient(IntPtr.Zero);
                _mmf_path_prefix = "abyst_" + origin.StandardUri.Host.Replace('.', '_').Replace(':', '_') + "_";
            }
            Origin = origin;
            IsValid = true;
        }
        public readonly bool IsValid;
        public readonly AbyssURL Origin;

        private readonly AbyssLib.AbystClient _abyst_client;
        private readonly string _mmf_path_prefix; //for file sharing with rendering engine.
        private readonly HttpClient _http_client = new(); //TODO: TLS client auth
        private readonly Dictionary<string, WaiterGroup<FileResource>> _media_cache = []; //registered when resource is requested.
        public class FileResource
        {
            public MemoryMappedFile MMF = null; //TODO: close this after removing the resource from the renderer
            public ABI.File ABIFileInfo = null;
            public bool IsValid = false; //must be only set from ResourceLoader
        }
        public bool TryGetFileOrWaiter(string url_string, MIME MimeType, out FileResource resource, out Waiter<FileResource> waiter)
        {
            if(!AbyssURLParser.TryParseFrom(url_string, Origin, out var url))
            {
                resource = new FileResource { IsValid = false };
                waiter = null;
                return true;
            }

            WaiterGroup<FileResource> waiting_group;
            bool require_loading;
            lock (_media_cache)
            {
                if (!_media_cache.TryGetValue(url.Raw, out waiting_group))
                {
                    waiting_group = new();
                    _media_cache.Add(url.Raw, waiting_group);
                    require_loading = true;
                }
                else
                {
                    require_loading = false;
                }
            }
            if (require_loading)
            {
                _ = Loadresource(url, MimeType, waiting_group); //do not wait.
            }

            return waiting_group.TryGetValueOrWaiter(out resource, out waiter);
        }
        public async Task<HttpResponseMessage> TryHttpGetAsync(string url_string)
        {
            if (!AbyssURLParser.TryParseFrom(url_string, Origin, out var url))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }

            return await TryHttpGetAsync(url);
        }
        public async Task<HttpResponseMessage> TryHttpPostAsync(string url_string, HttpContent content)
        {
            if (!AbyssURLParser.TryParseFrom(url_string, Origin, out var url))
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
            }

            return await TryHttpPostAsync(url, content);
        }
        public async Task<HttpResponseMessage> TryHttpGetAsync(AbyssURL url)
        {
            if (url.Scheme == "http" || url.Scheme == "https")
            {
                return await _http_client.GetAsync(url.Raw);
            }

            if (url.Scheme == "abyst" && _abyst_client.IsValid())
            {
                var raw_response = _abyst_client.Request(AbyssLib.AbystRequestMethod.GET, url.Path);
                if (!raw_response.TryLoadBodyAll())
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.UnprocessableContent);
                }

                var response = new HttpResponseMessage((System.Net.HttpStatusCode)raw_response.Code)
                {
                    Content = new ByteArrayContent(raw_response.Body),
                };
                foreach (var entry in raw_response.Header)
                {
                    switch (entry.Key)
                    {
                        case "Content-Length" or "Content-Type"
                        or "Content-Disposition" or "Content-Encoding"
                        or "Content-Language" or "Content-Location"
                        or "Content-MD5" or "Expires"
                        or "Last-Modified":
                            response.Content.Headers.Add(entry.Key, entry.Value);
                            break;
                        default:
                            response.Headers.Add(entry.Key, entry.Value);
                            break;
                    }
                }
                return response;
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }
        public async Task<HttpResponseMessage> TryHttpPostAsync(AbyssURL url, HttpContent content)
        {
            if (url.Scheme == "http" || url.Scheme == "https")
            {
                return await _http_client.PostAsync(url.Raw, content);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);
        }
        private async Task Loadresource(AbyssURL url, MIME MimeType, WaiterGroup<FileResource> dest)
        {
            var response = await TryHttpGetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                Client.Client.Cerr.WriteLine("failed to load resource(" + url.Raw + "): " + response.StatusCode.ToString());
                dest.TryFinalizeValue(default);
                return;
            }
            byte[] fileBytes = await response.Content.ReadAsByteArrayAsync();

            var component_id = RenderID.ComponentId;
            var mmf_path = _mmf_path_prefix + component_id.ToString();
            var mmf = MemoryMappedFile.CreateNew(mmf_path, fileBytes.Length);
            var accessor = mmf.CreateViewAccessor();
            accessor.WriteArray(0, fileBytes, 0, fileBytes.Length);
            accessor.Flush();
            accessor.Dispose();
            var abi_fileinfo = new ABI.File()
            {
                Mime = MimeType,
                MmapName = mmf_path,
                Off = 0,
                Len = (uint)fileBytes.Length,
            };

            if (!dest.TryFinalizeValue(new FileResource
            {
                IsValid = true,
                MMF = mmf,
                ABIFileInfo = abi_fileinfo,
            }))
            {
                throw new Exception("double load"); //should never happen.
            }
            //Client.Client.Cerr.WriteLine($"[INFO] resource size {fileBytes.Length} from {url.Raw}");
        }
    }
    public class RequestOptions
    {
        public string Method;
        public HttpContent Content;
        public byte[] Body;
    }
}
