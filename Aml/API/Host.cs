using AbyssCLI.Tool;

namespace AbyssCLI.Aml.API
{
    public class Host(AbyssURL path)
    {
#pragma warning disable IDE1006 //naming convention
        public string hash()
        {
            return Client.Client.Host.local_aurl.Id;
        }
        public string abyss_url()
        {
            return Client.Client.Host.local_aurl.Raw;
        }
        public string root_certificate()
        {
            return System.Text.Encoding.ASCII.GetString(Client.Client.Host.root_certificate);
        }
        public string handshake_key_certificate()
        {
            return System.Text.Encoding.ASCII.GetString(Client.Client.Host.handshake_key_certificate);
        }
        public void register_peer(string root_cert, string handshake_key_cert)
        {
            var root_cert_bytes = System.Text.Encoding.ASCII.GetBytes(root_cert);
            var handshake_key_cert_bytes = System.Text.Encoding.ASCII.GetBytes(handshake_key_cert);
            var err = Client.Client.Host.AppendKnownPeer(root_cert_bytes, handshake_key_cert_bytes);
            if (!err.Empty)
            {
                throw new Exception(err.Message); //this should never happen.
            }
        }
        public void connect(string abyss_url)
        {
            Client.Client.Host.OpenOutboundConnection(abyss_url);
        }
        public void move_world(string url)
        {
            if (!AbyssURLParser.TryParseFrom(url, _path, out var url_parsed))
            {
                Client.Client.Cerr.WriteLine("move_world: failed to parse url");
                return;
            }
            Client.Client.MoveWorld(url_parsed);
        }
#pragma warning restore IDE1006

        private readonly AbyssURL _path = path;
    }
}