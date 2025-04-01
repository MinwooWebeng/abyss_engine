using AbyssCLI.ABI;
using AbyssCLI.Aml;
using AbyssCLI.Tool;
using System.Text.Json;

namespace AbyssCLI.Client
{
    public class Client
    {
        private readonly BinaryReader _cin;
        private readonly RenderActionWriter _cout;
        private readonly StreamWriter _cerr;
        private readonly AbyssLib.SimplePathResolver _resolver;
        private AbyssLib.Host _host;
        private bool _is_running;

        private World _current_world;

        private readonly WorldPathMapper _world_map = new();
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
            _cout.LocalInfo(_host.local_aurl);

            _is_running = true;
            var concurrent_error_thread = new Thread(() =>
            {
                while (_is_running)
                {
                    Thread.Sleep(1000);
                    var err_msg = AbyssLib.GetError().ToString();
                    while (err_msg != "no error")
                    {
                        Console.WriteLine(err_msg);
                        _cerr.WriteLine(err_msg);
                    }
                }
            });
            concurrent_error_thread.Start();

            _current_world = new World(_host, _cout, _cerr, );

            try
            {
                while (true)
                {
                    UIActionHandle();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                _cerr.WriteLine(ex.ToString());
            }

            _is_running = false;
            concurrent_error_thread.Join();
        }
        private void UIActionHandle()
        {
            var message = ReadProtoMessage();
            switch (message.InnerCase)
            {
                case UIAction.InnerOneofCase.MoveWorld: OnMoveWorld(message.MoveWorld); return;
                case UIAction.InnerOneofCase.ShareContent: OnShareContent(message.ShareContent); return;
                case UIAction.InnerOneofCase.ConnectPeer: _host.OpenOutboundConnection(message.ConnectPeer.Aurl); return;
                case UIAction.InnerOneofCase.Kill:
                case UIAction.InnerOneofCase.None:
                case UIAction.InnerOneofCase.Init:
                default: throw new Exception("fatal: received invalid UI Action");
            }
        }
        private void OnMoveWorld(UIAction.Types.MoveWorld args)
        {
            _current_world.TryClose();

            if (!TryParseMaybeLocalAddress(args.WorldUrl, out var world_url))
            {
                _cerr.WriteLine("failed to parse address: " + args.WorldUrl);
                return;
            }

            if (_world_map.TryPopMapping("/", out var old_world))
            {
                _host.AndCloseWorld("/");
                if (!old_world.TryClose())
                {
                    _cerr.WriteLine("failed to close world: " + old_world.UUID);
                    return;
                }
            }
            switch (world_url.Scheme)
            {
                //open
                case AbyssAddress.EScheme.Http:
                case AbyssAddress.EScheme.Abyst:
                    if (_host.AndOpenWorld("/", world_url.String) != 0)
                    {
                        _cerr.WriteLine("failed to open world");
                    }
                    break;
                //join
                case AbyssAddress.EScheme.Abyss:
                    _host.AndJoin("/", world_url.String);
                    break;
            }
        }
        private void OnShareContent(UIAction.Types.ShareContent args)
        {
            if (!TryParseMaybeLocalAddress(args.Url, out var content_url))
            {
                _cerr.WriteLine("OnShareContent: failed to parse address: " + args.Url);
                return;
            }

            if (!_world_map.TryGetUUID("/", out var old_world_uuid) || !_world_map.TryGetWorld(old_world_uuid, out var old_world))
            {
                _cerr.WriteLine("OnShareContent: failed to find world");
                return;
            }
            var content_uuid = Guid.NewGuid().ToString();
            if (!old_world.TryAddLocalContent(content_uuid, content_url, args.InitialPosition))
            {
                _cerr.WriteLine("failed to share local content:" + content_url);
                return;
            }
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
        //Actual handler functions
        //peer join
        // 1) update world members.
        // 2) request SOM service to peer.
        // 3) initiate SOM service for join target.
        // 4) if there is my shared object for world, share
        // 5) on peer leave, terminate SOM service for peer.
        private void OnAndJoinSuccess(AndEvent and_event)
        {
            AbyssAddress world_url;
            try
            {
                world_url = new AbyssAddress(and_event.URL);
            }
            catch
            {
                _cerr.WriteLine("failed to parse world address: " + and_event.URL);
                return;
            }
            if (!(world_url.Scheme == AbyssAddress.EScheme.Abyst ||
                world_url.Scheme == AbyssAddress.EScheme.Http))
            {
                _cerr.WriteLine("invalid world address scheme: " + and_event.URL);
                return;
            }

            var new_world = new World(_host, _cout, _cerr, and_event.UUID, world_url);
            if (!_world_map.TryAddMapping(and_event.LocalPath, and_event.UUID, new_world))
            {
                _cerr.WriteLine("world already exist for local path: " + and_event.LocalPath);
                return;
            }
            if(!new_world.TryActivate())
            {
                _cerr.WriteLine("failed to activate world: " + and_event.UUID);
                return;
            }
        }
        private void OnAndPeerJoin(AndEvent and_event)
        {
            if(!_world_map.TryGetWorld(and_event.UUID, out World world))
            {
                _cerr.WriteLine("peer joined in non-existing world: " + and_event.UUID);
                return;
            }
            if(!world.TryAddPeer(and_event.PeerHash))
            {
                _cerr.WriteLine("failed to add peer to world: " + and_event.UUID);
                return;
            }
        }
        private void OnAndPeerLeave(AndEvent and_event)
        {
            if (!_world_map.TryGetWorld(and_event.UUID, out World world))
            {
                _cerr.WriteLine("peer leaving non-existing world: " + and_event.UUID);
                return;
            }
            if (!world.TryRemovePeer(and_event.PeerHash))
            {
                _cerr.WriteLine("failed to remove peer from world: " + and_event.UUID);
                return;
            }
        }
        private void OnSomRenew(SomEvent somEvent)
        {
            if (!_world_map.TryGetWorld(somEvent.WorldUUID, out World world))
            {
                _cerr.WriteLine("som for non-existing world: " + somEvent.WorldUUID);
                return;
            }
            Tuple<AbyssAddress, string, vec3>[] parsed_content_info;
            try
            {
                parsed_content_info = somEvent.ObjectsInfo.Select(x => new Tuple<AbyssAddress, string, vec3>(new AbyssAddress(x.Item1), x.Item2, AmlValueParser.ParseVec3(x.Item3))).ToArray();
            }
            catch (Exception e)
            {
                _cerr.WriteLine("invalid address in som renew peer contents: " + somEvent.PeerHash + e.Message);
                return;
            }
            if (!world.TryRenewPeerContent(somEvent.PeerHash, parsed_content_info))
            {
                _cerr.WriteLine("failed to renew peer contents: " + somEvent.PeerHash);
                return;
            }
        }
        private void OnSomAppend(SomEvent somEvent)
        {
            if (!_world_map.TryGetWorld(somEvent.WorldUUID, out World world))
            {
                _cerr.WriteLine("som for non-existing world: " + somEvent.WorldUUID);
                return;
            }
            if (!world.TryAddPeerContent(somEvent.PeerHash, somEvent.ObjectsInfo[0].Item2, new AbyssAddress(somEvent.ObjectsInfo[0].Item1), AmlValueParser.ParseVec3(somEvent.ObjectsInfo[0].Item3)))
            {
                _cerr.WriteLine("failed to add peer contents: " + somEvent.PeerHash + somEvent.ObjectsInfo);
                return;
            }
        }
        private void OnSomDelete(SomEvent somEvent)
        {
            if (!_world_map.TryGetWorld(somEvent.WorldUUID, out World world))
            {
                _cerr.WriteLine("som for non-existing world: " + somEvent.WorldUUID);
                return;
            }
            if (!world.TryRemovePeerContent(somEvent.PeerHash, somEvent.ObjectsInfo[0].Item2))
            {
                _cerr.WriteLine("failed to add peer contents: " + somEvent.PeerHash + somEvent.ObjectsInfo);
                return;
            }
        }
        private bool TryParseMaybeLocalAddress(string address, out AbyssAddress result)
        {
            result = null;
            try
            {
                result = new AbyssAddress(address);
                return true;
            }
            catch
            {
                if (address.Contains(':'))
                    return false;

                //consider as local address.
                result = new AbyssAddress(new AbyssAddress("abyst:" + _host.local_aurl).GetRelativeAddress(address));
                return true;
            }
        }
    }
}