﻿using AbyssCLI.ABI;
using AbyssCLI.Tool;

namespace AbyssCLI.Client
{
    public static partial class Client
    {
        public static AbyssLib.Host Host { get; private set; }
        public static readonly RenderActionWriter RenderWriter = new(Console.OpenStandardOutput())
        {
            AutoFlush = true
        };

        private static readonly BinaryReader _cin = new(Console.OpenStandardInput());
        private static readonly StreamWriter _cerr = new(Stream.Synchronized(Console.OpenStandardError()))
        {
            AutoFlush = true
        };
        private static AbyssLib.SimplePathResolver _resolver;
        private static World _current_world;
        private static readonly object _world_move_lock = new();
        public static void CerrWriteLine(string message) => _cerr.WriteLine(message);
        public static void Init()
        {
            if (AbyssLib.Init() != 0)
            {
                throw new Exception("failed to initialize abyssnet.dll");
            }
            _resolver = AbyssLib.NewSimplePathResolver();

            //Host Initialization
            var init_msg = ReadProtoMessage();
            if (init_msg.InnerCase != UIAction.InnerOneofCase.Init)
            {
                throw new Exception("host not initialized");
            }
            var abyst_server_path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ABYST\\" + init_msg.Init.Name;
            if (!Directory.Exists(abyst_server_path))
            {
                Directory.CreateDirectory(abyst_server_path);
            }
            Host = AbyssLib.OpenAbyssHost(init_msg.Init.RootKey.ToByteArray(), _resolver, AbyssLib.NewSimpleAbystServer(abyst_server_path));
            if (!Host.IsValid())
            {
                CerrWriteLine("host creation failed: " + AbyssLib.GetError().ToString());
                return;
            }
            RenderWriter.LocalInfo(Host.local_aurl.Raw, Host.local_aurl.Id);

            var default_world_url_raw = "abyst:" + Host.local_aurl.Id;
            if (!AbyssURLParser.TryParse(default_world_url_raw, out AbyssURL default_world_url))
            {
                CerrWriteLine("default world url parsing failed");
                return;
            }
            var net_world = Host.OpenWorld(default_world_url_raw);
            _current_world = new World(Host, net_world, default_world_url);
            _current_world.Start();
            if (!_resolver.TrySetMapping("", net_world.world_id).Empty)
                throw new Exception("faild to set path for initial world at default path");
        }
    }
}