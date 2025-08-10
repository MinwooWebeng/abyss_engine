using AbyssCLI.Cache;
using AbyssCLI.Tool;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System.Collections.Concurrent;

namespace AbyssCLI.AML
{
    internal class JavaScriptDispatcher
    {
        private readonly V8ScriptEngine _engine;
        private readonly BlockingCollection<(string, object)> _queue = []; // by default, 100 scripts can be queued at once
        private readonly Thread _thread;

        public JavaScriptDispatcher(V8RuntimeConstraints constraints, Document document, Console console)
        {
            _engine = new V8ScriptEngine(constraints);
            _engine.AddHostType("Vector3", typeof(Vector3));
            _engine.AddHostType("Quaternion", typeof(Quaternion));

            _engine.AddHostObject("document", document);
            _engine.AddHostObject("console", console);

            _engine.AddHostType("Event", typeof(Event.Event));
            _engine.AddHostType("KeyboardEvent", typeof(Event.KeyboardEvent));

            _thread = new Thread(new ParameterizedThreadStart(Run));
        }
        public bool TryEnqueue(string filename, object entry) =>
            _queue.TryAdd((filename, entry));
        public void Start(CancellationToken token) =>
            _thread.Start(token);
        public void Interrupt() => _engine.Interrupt(); // call after token cancellation to kill running script
        public void Join()
        {
            _thread.Join();
            _queue.Dispose();
            _engine.Dispose();
        }

        private async void Run(object token_)
        {
            var token = (CancellationToken)token_;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await RunOneScript(token);
                }
                catch (ScriptEngineException ex)
                {
                    Client.Client.CerrWriteLine($"javascript error: {ex.ErrorDetails}");
                }
                catch (OperationCanceledException) //token cancellation
                {
                    return;
                }
                catch (Exception ex)
                {
                    Client.Client.CerrWriteLine($"fatal::: {ex}");
                }
            }
        }
        private async Task RunOneScript(CancellationToken token)
        {
            switch (_queue.Take(token))
            {
            case (string _, string script_text):
            {
                _engine.Execute(new Microsoft.ClearScript.DocumentInfo("<script>"), script_text);
                break;
            }
            case (string file_name, TaskCompletionReference<CachedResource> script_ref):
            {
                CachedResource script_resource = await script_ref.Task.WaitAsync(token);
                if (script_resource is not Cache.Text)
                {
                    Client.Client.CerrWriteLine("invalid javascript resource");
                    return;
                }
                if (script_resource.MIMEType != "text/javascript")
                {
                    Client.Client.CerrWriteLine("javascript MIME mismatch: " + script_resource.MIMEType);
                    return;
                }
                string remote_script_text = await (script_resource as Cache.Text).ReadAsync(token);
                _engine.Execute(new Microsoft.ClearScript.DocumentInfo(file_name), remote_script_text);
                break;
            }
            default:
                throw new InvalidOperationException("Unsupported script type (fatal internal error)");
            }
        }
    }
}
