using AbyssCLI.Tool;
using System.Numerics;

namespace AbyssCLI.HL;

internal class Item
{
    public readonly string _sharer_hash;
    public readonly Guid _uuid;
    public readonly AbyssURL _url;
    private readonly ContextedTask.ContextedTaskRoot _ct_root = new();
    public readonly HL.Content _content;

    public Item(string sharer_hash, Guid uuid, AbyssURL URL, Vector3 spawn_pos, Quaternion spawn_rot)
    {
        _sharer_hash = sharer_hash;
        _uuid = uuid;
        _url = URL;
        _content = new(URL, new()
        {
            title = sharer_hash + ":" + uuid.ToString(),
            pos = new(spawn_pos),
            rot = new(spawn_rot),
            is_item = true,
            sharer_hash = sharer_hash,
            uuid = uuid
        });
    }
    public void Start() => _ct_root.Attach(_content);

    public void Stop() => _content.Stop();
}
