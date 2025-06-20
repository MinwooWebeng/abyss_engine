using AbyssCLI.Tool;
using Microsoft.ClearScript;

namespace AbyssCLI.Client
{
    public class World
    {
        private readonly AbyssLib.Host _host;
        private readonly AbyssLib.World _world;
        private readonly CAbstraction.Environment _environment;
        private readonly Dictionary<string, CAbstraction.Member> _members = []; //peer hash - [uuid - item]
        private readonly Dictionary<Guid, CAbstraction.Item> _local_items = []; //UUID - item
        private readonly object _lock = new();
        private readonly Thread _world_th;

        public World(AbyssLib.Host host, AbyssLib.World world, AbyssURL URL)
        {
            _host = host;
            _world = world;
            _environment = new(host, URL);
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
        public void ShareItem(Guid uuid, AbyssURL url, float[] transform)
        {
            var item = new CAbstraction.Item(_host, _host.local_aurl.Id, uuid, url, Aml.RenderID.ElementId, transform);
            item.Activate();

            lock (_lock)
            {
                _local_items[uuid] = item;
                foreach (var entry in _members)
                {
                    entry.Value.network_handle.AppendObjects([Tuple.Create(uuid, url.Raw, transform)]);
                }
            }
        }
        public void UnshareItem(Guid guid)
        {
            lock (_lock)
            {
                var item = _local_items[guid];
                item.CloseAsync();
                _local_items.Remove(guid);
                foreach (var member in _members)
                {
                    member.Value.network_handle.DeleteObjects([guid]);
                }
            }
        }
        public void Leave()
        {
            _environment.CloseAsync();
            if (_world.Leave() != 0)
            {
                Client.CerrWriteLine("failed to leave world");
            }
            _world_th.Join();

            foreach (var member in _members)
            {
                foreach (var item in member.Value.remote_items.Values)
                {
                    item.CloseAsync();
                }
            }
            foreach (var item in _local_items.Values)
            {
                item.CloseAsync();
            }
            _members.Clear(); //do we need this?
            _local_items.Clear(); //do we need this?
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
                if (!_members.TryAdd(member.hash, new(member)))
                {
                    Client.CerrWriteLine("failed to append peer; old peer session pends");
                    return;
                }
                var list_of_local_items = _local_items
                    .Select(kvp => Tuple.Create(kvp.Key, kvp.Value.URL.Raw, kvp.Value.spawn_transform))
                    .ToArray();
                if (list_of_local_items.Length != 0)
                {
                    member.AppendObjects(list_of_local_items);
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
                        Client.CerrWriteLine("failed to parse object url: " + gst.Item2);
                    }
                    return Tuple.Create(gst.Item1, abyss_url, gst.Item3);
                })
                .Where(gst => gst.Item2 != null)
                .ToList();

            lock(_lock)
            {
                if (!_members.TryGetValue(evnt.peer_hash, out var member))
                {
                    Client.CerrWriteLine("failed to find member");
                    return;
                }
                
                foreach (var obj in parsed_objects)
                {
                    var item = new CAbstraction.Item(_host, evnt.peer_hash, obj.Item1, obj.Item2, Aml.RenderID.ElementId, obj.Item3);
                    if (!member.remote_items.TryAdd(obj.Item1, item))
                    {
                        Client.CerrWriteLine("uid collision of objects appended from peer");
                        continue;
                    }

                    item.Activate();
                }
            }
        }
        private void OnMemberObjectDelete(AbyssLib.MemberObjectDelete evnt)
        {
            lock (_lock)
            {
                if (!_members.TryGetValue(evnt.peer_hash, out var member))
                {
                    Client.CerrWriteLine("failed to find member");
                    return;
                }

                foreach (var id in evnt.object_ids)
                {
                    if (!member.remote_items.Remove(id, out var item))
                    {
                        Client.CerrWriteLine("peer tried to delete unshared objects");
                        continue;
                    }
                    item.CloseAsync();
                }
            }
        }
        private void OnMemberLeave(string peer_hash)
        {
            lock(_lock)
            {
                if (!_members.Remove(peer_hash, out var value))
                {
                    Client.CerrWriteLine("non-existing peer leaved");
                    return;
                }

                foreach (var item in value.remote_items.Values)
                {
                    item.CloseAsync();
                }
            }
        }
    }
}
