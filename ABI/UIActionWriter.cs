using Google.Protobuf;
using static AbyssCLI.ABI.UIAction.Types;
using System.IO;
using System;

namespace AbyssCLI.ABI
{
    internal class UIActionWriter
    {
		public UIActionWriter(System.IO.Stream stream) {
			_out_stream = stream;
		}
		
public void Init
(
    ByteString root_key
)
=> Write(new UIAction()
{
    Init = new Init
    {
        RootKey = root_key
    }
});
public void Kill
(
    int code
)
=> Write(new UIAction()
{
    Kill = new Kill
    {
        Code = code
    }
});
public void MoveWorld
(
    string world_url
)
=> Write(new UIAction()
{
    MoveWorld = new MoveWorld
    {
        WorldUrl = world_url
    }
});
public void ShareContent
(
    string url
)
=> Write(new UIAction()
{
    ShareContent = new ShareContent
    {
        Url = url
    }
});
public void ConnectPeer
(
    string aurl
)
=> Write(new UIAction()
{
    ConnectPeer = new ConnectPeer
    {
        Aurl = aurl
    }
});

		public void Flush()
		{
			_out_stream.Flush();
		}

		private void Write(UIAction msg)
		{
			var msg_len = msg.CalculateSize();

			_out_stream.Write(BitConverter.GetBytes(msg_len));
			msg.WriteTo(_out_stream);

            if(AutoFlush)
            {
                _out_stream.Flush();
            }
		}
		public bool AutoFlush = false;
		private readonly System.IO.Stream _out_stream;

	}
}