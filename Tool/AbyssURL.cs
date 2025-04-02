namespace AbyssCLI.Tool
{
    public class AbyssURL
    {
        public string Raw { get; set; }
        public string Scheme { get; set; }
        public string Id { get; set; } = ""; // For abyss/abyst
        public List<(string Ip, int Port)> AddressCandidates { get; set; } = [];
        public string Path { get; set; } = ""; // For abyss/abyst
        public Uri StandardUri { get; set; } // For standard and abyst URIs
    }

    public static class AbyssURLParser
    {
        public static bool TryParse(string input, out AbyssURL result)
        {
            if (input.StartsWith("abyss:"))
            {
                return TryParseAbyss(input, out result);
            }
            else if (input.StartsWith("abyst:"))
            {
                return TryParseAbyst(input, out result);
            }
            else
            {
                try
                {
                    var parsed_uri = new Uri(input);
                    result = new AbyssURL
                    {
                        Raw = input,
                        Scheme = parsed_uri.Scheme,
                        StandardUri = parsed_uri,
                    };
                    return true;
                }
                catch
                {
                    result = new AbyssURL();
                    return false;
                }
            }
        }
        private const string Base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        private static bool IsValidPeerID(string input)
        {
            if (string.IsNullOrEmpty(input) || input.Length < 32 || !char.IsUpper(input[0])) //id version code
            {
                return false;
            }

            foreach (char c in input[1..])
            {
                if (!Base58Chars.Contains(c))
                    return false;
            }

            return true;
        }

        private static bool TryParseAbyss(string input, out AbyssURL result)
        {
            result = new AbyssURL
            {
                Raw = input,
                Scheme = "abyss"
            };

            string body = input["abyss:".Length..];
            if (string.IsNullOrEmpty(body))
            {
                return false;
            }

            var addr_start_pos = body.IndexOf(':');
            var path_start_pos = body.IndexOf('/');
            if (addr_start_pos == -1) //there is no address section.
            {

                if (path_start_pos == -1)
                {
                    //there is no path either, body is the id.
                    //check if body is a valid id.
                    if (IsValidPeerID(body)) //id version code
                    {
                        return false;
                    }
                    result.Id = body;
                    return true;
                }

                //only path.
                var _peer_id = body[..path_start_pos];
                if (IsValidPeerID(_peer_id)) //id version code
                {
                    return false;
                }
                result.Id = _peer_id;
                result.Path = body[(path_start_pos+1)..];
                return true;
            }

            //first, detach id that comes before addresses.
            var peer_id = body[..addr_start_pos];
            if (!IsValidPeerID(peer_id))
            {
                return false;
            }
            result.Id = peer_id;
            //now, it is also certain that path starts after the addresses, as the peer ID cannot contain '/'.

            var addr_part = path_start_pos != -1 ? body[(addr_start_pos + 1)..path_start_pos] : body[(addr_start_pos + 1)..];
            result.Path = path_start_pos != -1 ? body[(path_start_pos + 1)..] : "";

            // Parse IP:Port list
            foreach (var ep in addr_part.Split('|', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = ep.Split(':');
                if (parts.Length == 2 &&
                    System.Net.IPAddress.TryParse(parts[0], out _) &&
                    int.TryParse(parts[1], out int port))
                {
                    result.AddressCandidates.Add((parts[0], port));
                }
            }

            return true;
        }

        private static bool TryParseAbyst(string input, out AbyssURL result)
        {
            result = new AbyssURL { Scheme = "abyst" };
            string body = input["abyst:".Length..];

            // Extract ID and path/query using first '/'
            int slashIndex = body.IndexOf('/');
            if (slashIndex == -1)
            {
                if (!IsValidPeerID(body))
                {
                    return false;
                }
                result.Id = body;
                return true;
            }

            result.Path = body[(slashIndex+1)..];
            return true;
        }
    }
}
