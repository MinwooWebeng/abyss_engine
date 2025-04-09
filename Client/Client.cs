﻿using AbyssCLI.ABI;
using AbyssCLI.Tool;

namespace AbyssCLI.Client
{
    public static class Client
    {
        public static readonly RenderActionWriter RenderWriter = new(Stream.Synchronized(Console.OpenStandardOutput()));
        public static readonly StreamWriter Cerr = new(Stream.Synchronized(Console.OpenStandardError()))
        {
            AutoFlush = true
        };
        public static AbyssLib.Host Host { get; private set; }

        private static readonly BinaryReader _cin = new(Console.OpenStandardInput());
        private static AbyssLib.SimplePathResolver _resolver;
        private static World _current_world;
        private static readonly object _world_move_lock = new();
        public static void Run()
        {
            _resolver = AbyssLib.NewSimplePathResolver();
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
                Cerr.WriteLine(ex.ToString());
                return;
            }

            Host = AbyssLib.OpenAbyssHost(init_msg.Init.RootKey.ToByteArray(), _resolver, AbyssLib.NewSimpleAbystServer("D:\\WORKS\\github\\abyss_engine\\testground\\abyst_server"));
            if (!Host.IsValid())
            {
                Cerr.WriteLine("host creation failed: " + AbyssLib.GetError().ToString());
                return;
            }
            RenderWriter.LocalInfo(Host.local_aurl.Raw);

            var default_world_url_raw = "abyst:" + Host.local_aurl.Id;
            if (!AbyssURLParser.TryParse(default_world_url_raw, out AbyssURL default_world_url))
            {
                Cerr.WriteLine("default world url parsing failed");
                return;
            }
            var net_world = Host.OpenWorld(default_world_url_raw);
            _current_world = new World(Host, net_world, default_world_url);
            _resolver.SetMapping("", net_world.world_id);

            try
            {
                while (UIActionHandle()) { }
            }
            catch (Exception ex)
            {
                Cerr.WriteLine(ex.ToString());
            }
        }
        public static void MoveWorld(AbyssURL url) => MainWorldSwap(url);
        private static bool UIActionHandle()
        {
            var message = ReadProtoMessage();
            switch (message.InnerCase)
            {
                case UIAction.InnerOneofCase.MoveWorld: OnMoveWorld(message.MoveWorld); return true;
                case UIAction.InnerOneofCase.ShareContent: OnShareContent(message.ShareContent); return true;
                case UIAction.InnerOneofCase.ConnectPeer:
                    if (Host.OpenOutboundConnection(message.ConnectPeer.Aurl) != 0)
                    {
                        Cerr.WriteLine("failed to open outbound connection: " + message.ConnectPeer.Aurl);
                    }
                    return true;
                case UIAction.InnerOneofCase.Kill:
                    return false;
                case UIAction.InnerOneofCase.None:
                case UIAction.InnerOneofCase.Init:
                default: throw new Exception("fatal: received invalid UI Action");
            }
        }
        private static void OnMoveWorld(UIAction.Types.MoveWorld args)
        {
            if (!AbyssURLParser.TryParseFrom(args.WorldUrl, Host.local_aurl, out var aurl)) {
                Cerr.WriteLine("MoveWorld: failed to parse world url");
                return;
            }

            MainWorldSwap(aurl);
        }
        private static void MainWorldSwap(AbyssURL url)
        {
            lock (_world_move_lock)
            {
                AbyssLib.World net_world;
                AbyssURL world_url;
                if (url.Scheme == "abyss")
                {
                    net_world = Host.JoinWorld(url.Raw);
                    if (!AbyssURLParser.TryParse(net_world.url, out world_url) || world_url.Scheme == "abyss")
                    {
                        Cerr.WriteLine("invalid world url");
                        net_world.Leave();
                        return;
                    }
                }
                else
                {
                    net_world = Host.OpenWorld(url.Raw);
                    world_url = url;
                }
                if (!net_world.IsValid())
                {
                    Cerr.WriteLine("MoveWorld: failed to open world");
                    return;
                }

                _resolver.DeleteMapping("");
                _current_world.Leave();
                _current_world = new World(Host, net_world, world_url);
                _resolver.SetMapping("", net_world.world_id);
            }
        }
        private static void OnShareContent(UIAction.Types.ShareContent args)
        {
            if (!AbyssURLParser.TryParseFrom(args.Url, Host.local_aurl, out var content_url))
            {
                Cerr.WriteLine("OnShareContent: failed to parse address: " + args.Url);
                return;
            }

            _current_world.ShareObject(content_url, [args.Pos.X, args.Pos.Y, args.Pos.Z, args.Rot.W, args.Rot.X, args.Rot.Y, args.Rot.Z]);
        }
        private static UIAction ReadProtoMessage()
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