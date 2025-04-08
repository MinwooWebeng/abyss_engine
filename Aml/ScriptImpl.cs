using Microsoft.ClearScript;
using Microsoft.ClearScript.V8;
using System.Xml;

namespace AbyssCLI.Aml
{
    internal sealed class ScriptImpl : AmlNode
    {
        public ScriptImpl(AmlNode context, XmlNode xml_node, AbyssLib.Host host, DocumentImpl document)
            : base(context)
        {
            _host = host;
            _document = document;
            _script = xml_node.InnerText;
        }
        protected override Task ActivateSelfCallback(CancellationToken token)
        {
            _engine = new(
                new V8RuntimeConstraints()
                {
                    MaxOldSpaceSize = 32 * 1024 * 1024
                }
            );
            _engine.AddHostObject("host", new API.Host(_host));
            _engine.AddHostObject("document", new API.Document(_document));
            _engine.AddHostObject("console", new API.Console(ErrorStream));

            token.ThrowIfCancellationRequested();
            try
            {
                _engine.Execute(_script);
            }
            catch (ScriptEngineException ex)
            {
                this.ErrorStream.WriteLine("javascript error:" + ex.Message);
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

        private readonly AbyssLib.Host _host;
        private readonly DocumentImpl _document;
        private V8ScriptEngine _engine;
        private readonly string _script;
    }
}