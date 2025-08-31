﻿using AbyssCLI.Cache;
using AbyssCLI.Tool;
using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System.Collections.Concurrent;

#nullable enable
namespace AbyssCLI.AML;

public class JavaScriptGcCallback(ElementLifespanMan elem_lifespan_man)
{
    public void on_gc(int element_id)
    {
        Element elem = elem_lifespan_man.Find(element_id);
        elem.RefCount--;
        //Client.Client.RenderWriter.ConsolePrint("+++ JsEngine returned an element handle: " + element_id);
    }
}
public class JavaScriptDispatcher
{
    private readonly V8ScriptEngine _engine;
    private readonly BlockingCollection<(string, object)> _queue = []; // by default, 100 scripts can be queued at once
    private readonly Thread _thread;

    public JavaScriptDispatcher(V8RuntimeConstraints constraints, Document document, Console console, JavaScriptGcCallback gc_callback)
    {
        _engine = new V8ScriptEngine(constraints, V8ScriptEngineFlags.DisableGlobalMembers);
        _engine.AddHostType("Vector3", typeof(Vector3));
        _engine.AddHostType("Quaternion", typeof(Quaternion));

        _engine.AddHostObject("document", new JavaScriptAPI.Document(this, document));
        _engine.AddHostObject("console", console);

        _engine.AddHostType("Event", typeof(Event.Event));
        _engine.AddHostType("KeyboardEvent", typeof(Event.KeyboardEvent));

        _engine.AddHostObject("elem_gc_callback", gc_callback);

        _engine.Execute(@"
const version = '" + Tool.ExternData.BuildTime + @"';

const __aml_elem_finreg = new FinalizationRegistry(heldValue => elem_gc_callback.on_gc(heldValue));
function __aml_elem_dtor_reg(target, heldValue) {
    __aml_elem_finreg.register(target, heldValue);
    return target;
}
"
        );

        _thread = new Thread(new ParameterizedThreadStart(Run!));
    }
    public bool TryEnqueue(string filename, object entry) =>
        _queue.TryAdd((filename, entry));
    public void Start(CancellationToken token) =>
        _thread.Start(token);
    public void Interrupt() => _engine.Interrupt(); // call after token cancellation to kill running script
    public void Join()
    {
        if (_thread.IsAlive)
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
        case (string text_title, string script_text):
        {
            //Client.Client.RenderWriter.ConsolePrint("JsDispatcher: running " + (text_title.Length == 0 ? "<script>" : text_title));
            _engine.Execute(new Microsoft.ClearScript.DocumentInfo("<script>"), script_text);

            ///debug - GC
            _engine.CollectGarbage(true);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _engine.Execute("void 0");
            //Client.Client.RenderWriter.ConsolePrint("JsDispatcher: finished " + (text_title.Length == 0 ? "<script>" : text_title));
            break;
        }
        case (string file_name, TaskCompletionReference<CachedResource> script_ref):
        {
            //Client.Client.RenderWriter.ConsolePrint("JsDispatcher: loading " + file_name);
            CachedResource script_resource = await script_ref.Task.WaitAsync(token);
            //Client.Client.RenderWriter.ConsolePrint("JsDispatcher: running " + file_name);
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
            string remote_script_text = await (script_resource as Cache.Text)!.ReadAsync(token);
            _engine.Execute(new Microsoft.ClearScript.DocumentInfo(file_name), remote_script_text);
            //Client.Client.RenderWriter.ConsolePrint("JsDispatcher: finished " + file_name);
            break;
        }
        case (_, Action action):
            action();
            break;
        default:
            throw new InvalidOperationException("Unsupported script type (fatal internal error)");
        }
    }
    //for JavaScript API
    public object MarshalElement(AML.Element element)
    {
        //Client.Client.RenderWriter.ConsolePrint("++JsEngine claims an element handle: " + element.ElementId);
        element.RefCount++;
        JavaScriptAPI.Transform result = element switch
        {
            AML.Body body => new JavaScriptAPI.Body(this, body),
            AML.Transform transform => new JavaScriptAPI.Transform(this, transform),
            _ => throw new NotImplementedException()
        };
        return _engine.Script.__aml_elem_dtor_reg(result, element.ElementId);
    }
    public object[] MarshalElementArray(List<AML.Element> elements)
    {
        return elements.Select(MarshalElement)
           .ToArray()!;
    }
}
