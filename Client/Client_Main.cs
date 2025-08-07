using AbyssCLI.ABI;

namespace AbyssCLI.Client;

public partial class Client
{
    private static UIAction ReadProtoMessage()
    {
        int length = _cin.ReadInt32();
        byte[] data = _cin.ReadBytes(length);
        if (data.Length != length)
        {
            throw new Exception("stream closed");
        }
        return UIAction.Parser.ParseFrom(data);
    }

    private static bool UIActionHandle()
    {
        UIAction message = ReadProtoMessage();
        switch (message.InnerCase)
        {
        case UIAction.InnerOneofCase.Kill:
            return false;
        case UIAction.InnerOneofCase.MoveWorld: OnMoveWorld(message.MoveWorld); return true;
        case UIAction.InnerOneofCase.ShareContent: OnShareContent(message.ShareContent); return true;
        case UIAction.InnerOneofCase.UnshareContent: OnUnshareContent(message.UnshareContent); return true;
        case UIAction.InnerOneofCase.ConnectPeer: OnConnectPeer(message.ConnectPeer); return true;
        case UIAction.InnerOneofCase.ConsoleInput: OnConsoleInput(message.ConsoleInput); return true;
        default: throw new Exception("fatal: received invalid UI Action");
        }
    }

    public static void Start()
    {
        while (UIActionHandle()) { }
    }
}
