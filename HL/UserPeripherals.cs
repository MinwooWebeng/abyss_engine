using AbyssCLI.AmlDepr;
using AbyssCLI.Tool;

namespace AbyssCLI.HL;

internal class UserPeripherals
{
    [Obsolete]
    public UserPeripherals(AbyssLib.Host host, string peer_hash)
    {
        sharer_hash = peer_hash;
        if (!AbyssURLParser.TryParse("abyst:" + peer_hash, out AbyssURL aurl_base))
        {
            throw new Exception("failed to construct user peripherals loader");
        }
        resourceLoader = new(host, aurl_base);
        profile = new(resourceLoader);
    }
    public readonly string sharer_hash;
    private readonly ResourceLoader resourceLoader;
    public void Activate()
    {
        if (!resourceLoader.IsValid) return;
    }

    [Obsolete]
    public void Close() => profile.Close();

    [Obsolete]
    private readonly Profile profile;
}
