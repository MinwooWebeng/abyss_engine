namespace AbyssCLI.Aml.API
{
    public class Host(AbyssLib.Host host)
    {
#pragma warning disable IDE1006 //naming convention
        public string hash()
        {
            return _host.local_aurl.Id;
        }
        public string abyss_url()
        {
            return _host.local_aurl.Raw;
        }
        public string root_certificate()
        {
            return System.Text.Encoding.ASCII.GetString(_host.root_certificate);
        }
        public string handshake_key_certificate()
        {
            return System.Text.Encoding.ASCII.GetString(_host.handshake_key_certificate);
        }
        public bool register_peer(string root_cert, string handshake_key_cert)
        {
            try
            {
                var root_cert_bytes = System.Text.Encoding.ASCII.GetBytes(root_cert);
                var handshake_key_cert_bytes = System.Text.Encoding.ASCII.GetBytes(handshake_key_cert);
                return _host.AppendKnownPeer(root_cert_bytes, handshake_key_cert_bytes) == 0;
            }
            catch
            {
                return false;
            }
        }
        public void connect(string abyss_url)
        {
            _host.OpenOutboundConnection(abyss_url);
        }
#pragma warning restore IDE1006

        private readonly AbyssLib.Host _host = host;
    }
}