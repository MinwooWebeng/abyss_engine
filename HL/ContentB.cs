using AbyssCLI.AML;
using AbyssCLI.Tool;

#nullable enable
namespace AbyssCLI.HL;
internal class ContentB : IDisposable
{
    private readonly AbyssURL _url;
    internal readonly Document Document;
    private readonly CancellationTokenSource _cts;
    private readonly Task _content_task;
    internal ContentB(AbyssURL url, AmlMetadata metadata)
    {
        _url = url;
        Document = new(metadata);
        _cts = new();

        //TODO: properly handle all exceptions from content task.
        _content_task = Task.Run(async() =>
        {
            try
            {
                Document.Init();
                using var _document_cache_ref = Client.Client.Cache.GetReference(_url.ToString());
                var doc_resource = await _document_cache_ref.Task.WaitAsync(_cts.Token);
                if (doc_resource is not Cache.Text doc_text) //relaxed from text/aml, Cache.Text allows text/* - for compatibility
                {
                    throw new Exception("fatal:::MIME mismatch: " + (doc_resource.MIMEType == "" ? "<unspecified>" : doc_resource.MIMEType));
                }
                string raw_document = await doc_text.ReadAsync(_cts.Token);

                ParseUtil.ParseAMLDocument(Document, raw_document, _cts.Token);
                Document.StartJavaScript(_cts.Token);

                while (true)
                { //temporary: fixed duration cleanup
                    try
                    {
                        await Task.Delay(1000, _cts.Token);
                    }
                    catch
                    {
                        break;
                    }
                    Document.ScheduleOphanedElementCleanup();
                }

                Document.Interrupt();
                Document.Join();
            }
            catch (Exception ex)
            {
                Client.Client.RenderWriter.ConsolePrint("content throw an exception: " + ex.ToString());
            }
        });
    }

    private bool is_disposed;
    public void Dispose()
    {
        if (is_disposed)
            return;

        _cts.Cancel();
        try
        {
            _content_task.Wait();
        }
        catch (Exception ex)
        {
            Client.Client.RenderWriter.ConsolePrint("***FATAL***: uncaught exception from content: " + ex.ToString());
        }
        Document.Join();

        is_disposed = true;
    }
}
