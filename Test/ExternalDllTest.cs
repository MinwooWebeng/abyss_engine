namespace AbyssCLI.Test
{
    public static class ExternalDllTest
    {
        public static void TestDllLoad()
        {
            Console.WriteLine(AbyssLib.GetVersion());
            Console.WriteLine(AbyssLib.GetError().ToString());
        }

        public static void TestHostCreate()
        {
            var priv_key = File.ReadAllBytes("test_key1.pem");
            var path_res = AbyssLib.NewSimplePathResolver();
            var host = AbyssLib.OpenAbyssHost(priv_key, path_res);
            Console.WriteLine(host.IsValid() ? "success" : "fail");
        }
        public static void TestHostJoin()
        {
            var priv_key1 = File.ReadAllBytes("test_key1.pem");
            var priv_key2 = File.ReadAllBytes("test_key2.pem");

            var path_res1 = AbyssLib.NewSimplePathResolver();
            var path_res2 = AbyssLib.NewSimplePathResolver();

            var host1 = AbyssLib.OpenAbyssHost(priv_key1, path_res1);
            var host2 = AbyssLib.OpenAbyssHost(priv_key2, path_res2);

            host1.AppendKnownPeer(host2.root_certificate, host2.handshake_key_certificate);
            host2.AppendKnownPeer(host1.root_certificate, host1.handshake_key_certificate);

            var world1 = host1.OpenWorld("plain.world");
            if (!world1.IsValid())
            {
                Console.WriteLine("failed to open world");
                return;
            }
            path_res1.SetMapping("/cat", world1.world_id);

            Thread.Sleep(1000);

            var host1_th = new Thread(() =>
            {
                var err = host1.OpenOutboundConnection(host2.local_aurl);

                Console.WriteLine("b1");
                var evnt_raw = world1.WaitForEvent();
                {
                    AbyssLib.WorldMemberRequest evnt = evnt_raw as AbyssLib.WorldMemberRequest;
                    evnt.Accept();
                }
                Console.WriteLine("b2");
                evnt_raw = world1.WaitForEvent();
                {
                    if (evnt_raw is AbyssLib.WorldMember evnt)
                    {
                        Console.WriteLine("Success(1)!");
                    }
                }
            });
            host1_th.Start();

            Console.WriteLine(host1.local_aurl);
            var world = host2.JoinWorld(host1.local_aurl + "cat");
            if (world.IsValid())
            {
                Console.WriteLine("c1");
                var evnt_raw = world.WaitForEvent();
                {
                    AbyssLib.WorldMemberRequest evnt = evnt_raw as AbyssLib.WorldMemberRequest;
                    evnt.Accept();
                }
                Console.WriteLine("c2");
                evnt_raw = world.WaitForEvent();
                {
                    if (evnt_raw is AbyssLib.WorldMember evnt)
                    {
                        Console.WriteLine("Success(2)!");
                    }
                }
            } 
            else
            {
                Console.WriteLine("failed to join world");
            }

            host1_th.Join();
        }
    }
}
