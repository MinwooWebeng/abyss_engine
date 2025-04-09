using Google.Protobuf.WellKnownTypes;
using Microsoft.ClearScript;
using Microsoft.ClearScript.JavaScript;
using Microsoft.ClearScript.V8;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class ScriptImpl(AmlNode context, XmlNode xml_node, AbyssLib.Host host, DocumentImpl document) : AmlNode(context)
    {
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            _engine = new(
                new V8RuntimeConstraints()
                {
                    MaxOldSpaceSize = 32 * 1024 * 1024
                }
            );
            _engine.Script.host = new API.Host(ResourceLoader.Origin);
            _engine.Script.document = new API.Document(_document);
            _engine.Script.console = new API.Console(Client.Client.Cerr);
            _engine.Script.sleep = new Func<int, object>((int ms) => JavaScriptExtensions.ToPromise(Task.Delay(ms)));

            var fetch_api = new API.WebFetchApi(ResourceLoader);
            _engine.Script.__fetch_s = new Func<string, object>(
                (string resource) => JavaScriptExtensions.ToPromise(fetch_api.FetchAsync(resource))
            );
            _engine.Script.__fetch_d = new Func<string, object, object>(
                (string resource, object options) => JavaScriptExtensions.ToPromise(fetch_api.FetchAsync(resource, options))
            );
            _engine.Execute("function fetch(...args){if(args.length===1){return __fetch_s(args[0]);}else if(args.length===2){return __fetch_d(args[0],args[1]);}throw new Error('fetch() requires either 1 or 2 arguments.');}; ");

            token.ThrowIfCancellationRequested();
            try
            {
                _engine.Execute(_script);
            }
            catch (ScriptEngineException ex)
            {
                Client.Client.Cerr.WriteLine("javascript error:" + ex.Message);
            }
            return Task.CompletedTask;
        }
        protected override void DeceaseSelfCallback()
        {
            _engine.Interrupt();
        }
        protected override void CleanupSelfCallback()
        {
            _engine.Dispose();
        }
        //TODO: find out a safer way of killing V8 after decease.
        public static string Tag => "script";

        private readonly AbyssLib.Host _host = host;
        private readonly DocumentImpl _document = document;
        private V8ScriptEngine _engine;
        private readonly string _script = xml_node.InnerText;
    }
}