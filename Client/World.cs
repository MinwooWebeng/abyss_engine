using AbyssCLI.Tool;

namespace AbyssCLI.Client
{
    public class World
    {
        private readonly AbyssLib.Host _host;
        private readonly AbyssLib.World _world;
        private readonly StreamWriter _cerr;
        private readonly Aml.Content _environment;
        private readonly Dictionary<string, Tuple<AbyssLib.WorldMember, Dictionary<Guid, Aml.Content>>> _members = []; //peer hash - [uuid - content]
        private readonly Dictionary<Guid, Aml.Content> _local_contents = []; //UUID - content
        private readonly object _lock = new();
        private readonly Thread _world_th;

        private static readonly float[] _defaultTransform = [0.0f, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f, 0.0f];
        public World(AbyssLib.Host host, AbyssLib.World world, AbyssURL URL)
        {
            _host = host;
            _world = world;
            _environment = new(host, URL, _defaultTransform);
            _environment.Activate();

            _world_th = new Thread(() =>
            {
                while (true)
                {
                    var evnt_raw = world.WaitForEvent();
                    switch (evnt_raw)
                    {
                        case AbyssLib.WorldMemberRequest evnt:
                            OnMemberRequest(evnt);
                            break;
                        case AbyssLib.WorldMember evnt:
                            OnMemberReady(evnt);
                            break;
                        case AbyssLib.MemberObjectAppend evnt:
                            OnMemberObjectAppend(evnt);
                            break;
                        case AbyssLib.MemberObjectDelete evnt:
                            OnMemberObjectDelete(evnt);
                            break;
                        case AbyssLib.WorldMemberLeave evnt:
                            OnMemberLeave(evnt.peer_hash);
                            break;
                        case int: //world termination
                            return;
                    }
                }
            });
            _world_th.Start();
        }
        public Guid ShareObject(AbyssURL url, float[] transform)
        {
            var guid = Guid.NewGuid();
            var content = new Aml.Content(_host, url, transform);
            content.Activate();

            lock (_lock)
            {
                _local_contents[guid] = content;
                foreach (var entry in _members)
                {
                    entry.Value.Item1.AppendObjects([Tuple.Create(guid, url.Raw, transform)]);
                }
            }
            return guid;
        }
        public void RemoveObject(Guid guid)
        {
            lock (_lock)
            {
                _local_contents[guid].Close();
                _local_contents.Remove(guid);

                foreach (var member in _members)
                {
                    member.Value.Item1.DeleteObjects([guid]);
                }
            }
        }
        public void Leave()
        {
            if (_world.Leave() != 0)
            {
                _cerr.WriteLine("failed to leave world");
            }
            _world_th.Join();

            _environment.Close();
            foreach (var member in _members)
            {
                foreach (var content in member.Value.Item2.Values)
                {
                    content.Close();
                }
            }
            foreach (var content in _local_contents.Values)
            {
                content.Close();
            }
            _members.Clear(); //do we need this?
            _local_contents.Clear(); //do we need this?
        }

        //internals
        private static void OnMemberRequest(AbyssLib.WorldMemberRequest evnt)
        {
            evnt.Accept();
        }
        private void OnMemberReady(AbyssLib.WorldMember member)
        {
            lock (_lock)
            {
                if (!_members.TryAdd(member.hash, Tuple.Create(member, new Dictionary<Guid, Aml.Content>())))
                {
                    _cerr.WriteLine("failed to append peer; old peer session pends");
                    return;
                }
                member.AppendObjects(_local_contents
                    .Select(kvp => Tuple.Create(kvp.Key, kvp.Value.URL.Raw, kvp.Value.Transform))
                    .ToArray());
            }
        }
        private void OnMemberObjectAppend(AbyssLib.MemberObjectAppend evnt)
        {
            var parsed_objects = evnt.objects
                .Select(gst =>
                {
                    if (!AbyssURLParser.TryParse(gst.Item2, out var abyss_url))
                    {
                        _cerr.WriteLine("failed to parse object url: " + gst.Item2);
                    }
                    return Tuple.Create(gst.Item1, abyss_url);
                })
                .Where(gst => gst.Item2 != null)
                .ToList();

            lock(_lock)
            {
                if (!_members.TryGetValue(evnt.peer_hash, out var member))
                {
                    _cerr.WriteLine("failed to find member");
                    return;
                }
                
                foreach (var obj in parsed_objects)
                {
                    var content = new Aml.Content(_host, obj.Item2, _defaultTransform);
                    if (!member.Item2.TryAdd(obj.Item1, content))
                    {
                        _cerr.WriteLine("uid collision of objects appended from peer");
                        continue;
                    }

                    content.Activate();
                }
            }
        }
        private void OnMemberObjectDelete(AbyssLib.MemberObjectDelete evnt)
        {
            lock (_lock)
            {
                if (!_members.TryGetValue(evnt.peer_hash, out var member))
                {
                    _cerr.WriteLine("failed to find member");
                    return;
                }

                foreach (var id in evnt.object_ids)
                {
                    if (!member.Item2.Remove(id, out var content))
                    {
                        _cerr.WriteLine("peer tried to delete unshared objects");
                        continue;
                    }
                    content.Close();
                }
            }
        }
        private void OnMemberLeave(string peer_hash)
        {
            lock(_lock)
            {
                if (!_members.Remove(peer_hash, out var value))
                {
                    _cerr.WriteLine("non-existing peer leaved");
                    return;
                }

                foreach (var items in value.Item2)
                {
                    items.Value.Close();
                }
            }
        }
    }
}
