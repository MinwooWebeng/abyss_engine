using AbyssCLI.ABI;
using AbyssCLI.Tool;
using Microsoft.VisualBasic;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace AbyssCLI.Aml
{
    //each content must have one MediaLoader
    //TODO: add CORS protection before adding cookie.
    internal class ResourceLoader(
        AbyssLib.Host host,
        StreamWriter cerr,
        AbyssURL origin)
    {
        private readonly AbyssLib.Host _host = host;
        private readonly AbyssLib.AbystClient _abyst_client = origin.Scheme == "abyst" ? host.GetAbystClient(origin.Id) : new AbyssLib.AbystClient(IntPtr.Zero);
        private readonly StreamWriter _cerr = cerr;
        private readonly AbyssURL _origin = origin;
        private readonly string _mmf_path_prefix = "abyst" + RanStr.RandomString(10); //for file sharing with rendering engine.
        private readonly HttpClient _http_client = new();
        private readonly Dictionary<string, WaiterGroup<FileResource>> _media_cache = []; //registered when resource is requested.
        public class FileResource
        {
            public MemoryMappedFile MMF = null; //TODO: close this after removing the resource from the renderer
            public ABI.File ABIFileInfo = null;
            public bool IsValid = false; //must be only set from ResourceLoader
        }
        public bool TryGetFileOrWaiter(string url_string, MIME MimeType, out FileResource resource, out Waiter<FileResource> waiter)
        {
            if(!AbyssURLParser.TryParseFrom(url_string, _origin, out var url))
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
        public async Task<Tuple<byte[], bool>> TryHttpRequestAsync(string url_string)
        {
            if (!AbyssURLParser.TryParseFrom(url_string, _origin, out var url))
            {
                return Tuple.Create<byte[], bool>([], false);
            }

            return await TryHttpRequestAsync(url);
        }
        public async Task<Tuple<byte[], bool>> TryHttpRequestAsync(AbyssURL url)
        {
            if (url.Scheme == "http" || url.Scheme == "https")
            {
                var httpResponse = await _http_client.GetAsync(url.Raw);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    return Tuple.Create<byte[], bool>([], false);
                }
                return Tuple.Create(await httpResponse.Content.ReadAsByteArrayAsync(), true);
            }

            if (url.Scheme == "abyst" || _abyst_client.IsValid())
            {
                var response = _abyst_client.Request(AbyssLib.AbystRequestMethod.GET, url.Path);
                if (response.IsValid() && response.TryLoadBodyAll())
                {
                    return Tuple.Create(response.Body, true);
                }
            }
            return Tuple.Create<byte[], bool>([], false);
        }
        private async Task Loadresource(AbyssURL url, MIME MimeType, WaiterGroup<FileResource> dest)
        {
            var response = await TryHttpRequestAsync(url);
            if (!response.Item2)
            {
                _cerr.WriteLine("failed to load resource: " + url.Raw);
                dest.TryFinalizeValue(default);
                return;
            }
            byte[] fileBytes = response.Item1;

            if (dest.IsFinalized()) //double load. should never happen.
            {
                throw new Exception("double load");
            }

            //should never throw from here.
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

            dest.TryFinalizeValue(new FileResource
            {
                IsValid = true,
                MMF = mmf,
                ABIFileInfo = abi_fileinfo,
            });
        }
    }
}
