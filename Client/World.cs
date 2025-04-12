using AbyssCLI.Aml;
using AbyssCLI.Tool;
using System.Text.RegularExpressions;

namespace AbyssCLI.Client
{
    public class World
    {
        private readonly AbyssLib.Host _host;
        private readonly AbyssLib.World _world;
        private readonly Aml.Content _environment;
        private readonly Dictionary<string, Tuple<AbyssLib.WorldMember, Dictionary<Guid, Aml.Content>>> _members = []; //peer hash - [uuid - content]
        private readonly Dictionary<Guid, Aml.Content> _local_contents = []; //UUID - content
        private readonly object _lock = new();
        private readonly Thread _world_th;

        public World(AbyssLib.Host host, AbyssLib.World world, AbyssURL URL)
        {
            _host = host;
            _world = world;
            _environment = new(host, URL, Aml.DocumentImpl._defaultTransform);
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
            Aml.Content content;
            try
            {
                content = new Aml.Content(_host, url, transform);
            }
            catch (Exception ex)
            {
                Client.Cerr.WriteLine("shared object construction failed: " + ex.Message);
                return Guid.Empty;
            }
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
                Client.Cerr.WriteLine("failed to leave world");
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
                    Client.Cerr.WriteLine("failed to append peer; old peer session pends");
                    return;
                }
                var list_of_local_contents = _local_contents
                    .Select(kvp => Tuple.Create(kvp.Key, kvp.Value.URL.Raw, kvp.Value.Transform))
                    .ToArray();
                if (list_of_local_contents.Length != 0)
                {
                    member.AppendObjects(list_of_local_contents);
                }
            }
        }
        private void OnMemberObjectAppend(AbyssLib.MemberObjectAppend evnt)
        {
            var parsed_objects = evnt.objects
                .Select(gst =>
                {
                    if (!AbyssURLParser.TryParse(gst.Item2, out var abyss_url))
                    {
                        Client.Cerr.WriteLine("failed to parse object url: " + gst.Item2);
                    }
                    return Tuple.Create(gst.Item1, abyss_url, gst.Item3);
                })
                .Where(gst => gst.Item2 != null)
                .ToList();

            lock(_lock)
            {
                if (!_members.TryGetValue(evnt.peer_hash, out var member))
                {
                    Client.Cerr.WriteLine("failed to find member");
                    return;
                }
                
                foreach (var obj in parsed_objects)
                {
                    Aml.Content content;
                    try
                    {
                        content = new Aml.Content(_host, obj.Item2, obj.Item3);
                    }
                    catch (Exception ex)
                    {
                        Client.Cerr.WriteLine("peer shared object construction failed: " + ex.Message);
                        continue;
                    }

                    if (!member.Item2.TryAdd(obj.Item1, content))
                    {
                        Client.Cerr.WriteLine("uid collision of objects appended from peer");
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
                    Client.Cerr.WriteLine("failed to find member");
                    return;
                }

                foreach (var id in evnt.object_ids)
                {
                    if (!member.Item2.Remove(id, out var content))
                    {
                        Client.Cerr.WriteLine("peer tried to delete unshared objects");
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
                    Client.Cerr.WriteLine("non-existing peer leaved");
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
