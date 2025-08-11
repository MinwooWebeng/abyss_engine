using AbyssCLI.AML;
using System.Buffers;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;

namespace AbyssCLI.Cache
{
    /// <summary>
    /// StaticResource in a IPC memory-mapped file with dynamically updating StaticResourceHeader prefix.
    /// </summary>
    public class StaticResource : CachedResource
    {
        public readonly int ResourceID = RenderID.ResourceId;
        const int BufferSize = 1024 * 1024 * 2; //2MB
        private TimeSpan RefreshDuration = TimeSpan.FromMilliseconds(200);
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource<bool> _done = new();
        private readonly MemoryMappedFile _mmf;
        private readonly string _name = GetRandomName();
        private readonly MemoryMappedViewAccessor _accessor;
        public StaticResource(HttpResponseMessage http_response) : base(http_response)
        {
            var mime_type = http_response.Content.Headers.ContentType?.MediaType ?? string.Empty;
            var content_length = (int)(_http_response.Content.Headers.ContentLength ?? 0);
            _mmf = MemoryMappedFile.CreateNew(
                _name,
                Marshal.SizeOf<StaticResourceHeader>() + content_length
            );
            _accessor = _mmf.CreateViewAccessor();
            var header = new StaticResourceHeader
            {
                TotalSize = content_length,
                CurrentSize = 0,
                IsLoading = true
            };
            _accessor.Write(0, ref header);
            Client.Client.RenderWriter.OpenStaticResource(ResourceID, GetMimeType(mime_type), _name);
            _ = LoadLoop();
        }
        private async Task LoadLoop()
        {
            var token = _cts.Token;
            var reader = await _http_response.Content.ReadAsStreamAsync(token);
            var content_length = (int)(_http_response.Content.Headers.ContentLength ?? 0);
            var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(BufferSize, content_length));
            var header = new StaticResourceHeader
            {
                TotalSize = content_length,
                CurrentSize = 0,
                IsLoading = true
            };
            try
            {
                while (true)
                {
                    using var refresh_cts = new CancellationTokenSource(RefreshDuration);
                    RefreshDuration *= 1.2;
                    using var refresh_or_cancel_cts = CancellationTokenSource.CreateLinkedTokenSource(token, refresh_cts.Token);
                    var refresh_token = refresh_or_cancel_cts.Token;
                    var prev_pos = reader.Position;
                    try
                    {
                        _ = await reader.ReadAsync(buffer, refresh_token);
                    }
                    catch (Exception e)
                    {
                        if ((e is TaskCanceledException tce) && (tce.CancellationToken == refresh_token))
                        {
                            // time to update progress. OK to proceed.
                        }
                        else
                        {
                            break;
                        }
                    }
                    var read_amount = (int)(reader.Position - prev_pos);
                    var next_CurrentSize = header.CurrentSize + read_amount;
                    if (next_CurrentSize > header.TotalSize)
                    {
                        Client.Client.CerrWriteLine("Received over Content-Length. faulty server");
                        break;
                    }

                    _accessor.WriteArray(
                        Marshal.SizeOf<StaticResourceHeader>() + header.CurrentSize,
                        buffer,
                        0,
                        (int)read_amount
                    );
                    header.CurrentSize = next_CurrentSize;

                    if (header.CurrentSize == header.TotalSize)
                        break;
                }
            }
            catch (Exception ex)
            {
                Client.Client.CerrWriteLine("fatal:::StaticResource.LoadLoop throwed an unexpected exception: " + ex.Message);
            }
            header.IsLoading = false;
            _accessor.Write(0, ref header);
            _http_response.Dispose();
            _done.SetResult(true);
        }
        private static readonly Random random = new();
        private static string GetRandomName()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < 20; i++)
            {
                _ = stringBuilder.Append(chars[random.Next(chars.Length)]);
            }
            return stringBuilder.ToString();
        }
        protected override void Dispose(bool _)
        {
            _cts.Cancel();
            _done.Task.Wait();
            _mmf.Dispose();
            Client.Client.RenderWriter.CloseResource(ResourceID);
        }
        private static MIME GetMimeType(string mime_type)
        {
            return mime_type switch
            {
                "image/png" => MIME.ImagePng,
                "image/jpeg" => MIME.ImageJpeg,
                "image/gif" => MIME.ImageGif,
                _ => MIME.Invalid
            };
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct StaticResourceHeader
    {
        public int TotalSize; // 4 byte header for total size
        public int CurrentSize; // 4 byte header for current size
        public bool IsLoading;
    }
}
