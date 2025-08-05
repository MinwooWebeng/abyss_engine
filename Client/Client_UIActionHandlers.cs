using AbyssCLI.ABI;
using AbyssCLI.Tool;

namespace AbyssCLI.Client;

public static partial class Client
{
    [Obsolete]
    private static void OnMoveWorld(UIAction.Types.MoveWorld args)
    {
        if (!AbyssURLParser.TryParseFrom(args.WorldUrl, Host.local_aurl, out AbyssURL aurl))
        {
            CerrWriteLine("MoveWorld: failed to parse world url");
            return;
        }
        MainWorldSwap(aurl);
    }

    [Obsolete]
    private static void OnShareContent(UIAction.Types.ShareContent args)
    {
        if (!AbyssURLParser.TryParseFrom(args.Url, Host.local_aurl, out AbyssURL content_url))
        {
            CerrWriteLine("OnShareContent: failed to parse address: " + args.Url);
            return;
        }
        _current_world.ShareItem(new Guid(args.Uuid.ToByteArray()), content_url, [args.Pos.X, args.Pos.Y, args.Pos.Z, args.Rot.W, args.Rot.X, args.Rot.Y, args.Rot.Z]);
    }

    [Obsolete]
    private static void OnUnshareContent(UIAction.Types.UnshareContent args) => _current_world.UnshareItem(new Guid(args.Uuid.ToByteArray()));
    private static void OnConnectPeer(UIAction.Types.ConnectPeer args)
    {
        if (Host.OpenOutboundConnection(args.Aurl) != 0)
        {
            CerrWriteLine("failed to open outbound connection: " + args.Aurl);
        }
    }
}
