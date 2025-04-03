using AbyssCLI.ABI;
using AbyssCLI.Tool;

namespace AbyssCLI.Client
{
    public class Client
    {
        private readonly BinaryReader _cin;
        private readonly RenderActionWriter _cout;
        private readonly StreamWriter _cerr;
        private readonly AbyssLib.SimplePathResolver _resolver;
        private AbyssLib.Host _host;

        private World _current_world;
        public Client()
        {
            _cin = new BinaryReader(Console.OpenStandardInput());
            _cout = new RenderActionWriter(Console.OpenStandardOutput());
            _cerr = new StreamWriter(Stream.Synchronized(Console.OpenStandardError()))
            {
                AutoFlush = true
            };
            _resolver = AbyssLib.NewSimplePathResolver();
        }
        public void Run()
        {
            UIAction init_msg;
            try
            {
                //Host Initialization
                init_msg = ReadProtoMessage();
                if (init_msg.InnerCase != UIAction.InnerOneofCase.Init)
                {
                    throw new Exception("fatal: host not initialized");
                }
            }
            catch (Exception ex)
            {
                _cerr.WriteLine(ex.ToString());
                return;
            }

            _host = AbyssLib.OpenAbyssHost(init_msg.Init.RootKey.ToByteArray(), _resolver);
            if (!_host.IsValid())
            {
                var err = AbyssLib.GetError();
                _cerr.WriteLine("host creation failed: " + err.ToString());
                return;
            }
            _cout.LocalInfo(_host.local_aurl.Raw);

            var default_world_url_raw = "abyst:" + _host.local_aurl.Id;
            if (!AbyssURLParser.TryParse(default_world_url_raw, out AbyssURL default_world_url))
            {
                _cerr.WriteLine("default world url parsing failed");
                return;
            }
            var net_world =  _host.OpenWorld(default_world_url_raw);
            _current_world = new World(_host, net_world, _cout, _cerr, default_world_url);
            _resolver.SetMapping("", net_world.world_id);

            try
            {
                while (UIActionHandle()){}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _cerr.WriteLine(ex.ToString());
            }
        }
        private bool UIActionHandle()
        {
            var message = ReadProtoMessage();
            switch (message.InnerCase)
            {
                case UIAction.InnerOneofCase.MoveWorld: OnMoveWorld(message.MoveWorld); return true;
                case UIAction.InnerOneofCase.ShareContent: OnShareContent(message.ShareContent); return true;
                case UIAction.InnerOneofCase.ConnectPeer: 
                    if (_host.OpenOutboundConnection(message.ConnectPeer.Aurl) != 0)
                    {
                        _cerr.WriteLine("failed to open outbound connection: " + message.ConnectPeer.Aurl);
                    }
                    return true;
                case UIAction.InnerOneofCase.Kill:
                    return false;
                case UIAction.InnerOneofCase.None:
                case UIAction.InnerOneofCase.Init:
                default: throw new Exception("fatal: received invalid UI Action");
            }
        }
        private void OnMoveWorld(UIAction.Types.MoveWorld args)
        {
            if (!AbyssURLParser.TryParseFrom(args.WorldUrl, _host.local_aurl, out var aurl)) {
                _cerr.WriteLine("MoveWorld: failed to parse world url");
                return;
            }

            AbyssLib.World net_world;
            AbyssURL world_url;
            if (aurl.Scheme == "abyss")
            {
                net_world = _host.JoinWorld(aurl.Raw);
                if (!AbyssURLParser.TryParse(net_world.url, out world_url) || world_url.Scheme == "abyss")
                {
                    _cerr.WriteLine("invalid world url");
                    net_world.Leave();
                    return;
                }
            }
            else
            {
                net_world = _host.OpenWorld(aurl.Raw);
                world_url = aurl;
            }
            if (!net_world.IsValid())
            {
                _cerr.WriteLine("MoveWorld: failed to open world");
                return;
            }

            _resolver.DeleteMapping("");
            _current_world.Leave();
            _current_world = new World(_host, net_world, _cout, _cerr, world_url);
            _resolver.SetMapping("", net_world.world_id);
        }
        private void OnShareContent(UIAction.Types.ShareContent args)
        {
            if (!AbyssURLParser.TryParseFrom(args.Url, _host.local_aurl, out var content_url))
            {
                _cerr.WriteLine("OnShareContent: failed to parse address: " + args.Url);
                return;
            }

            _current_world.ShareObject(content_url);
        }
        private UIAction ReadProtoMessage()
        {
            int length = _cin.ReadInt32();
            byte[] data = _cin.ReadBytes(length);
            if (data.Length != length)
            {
                throw new Exception("stream closed");
            }
            return UIAction.Parser.ParseFrom(data);
        }
    }
}