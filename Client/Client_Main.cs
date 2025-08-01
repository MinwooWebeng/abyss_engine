﻿using AbyssCLI.ABI;
using AbyssCLI.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AbyssCLI.Client
{
    partial class Client
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
            var message = ReadProtoMessage();
            switch (message.InnerCase)
            {
                case UIAction.InnerOneofCase.Kill:
                    return false;
                case UIAction.InnerOneofCase.MoveWorld: OnMoveWorld(message.MoveWorld); return true;
                case UIAction.InnerOneofCase.ShareContent: OnShareContent(message.ShareContent); return true;
                case UIAction.InnerOneofCase.UnshareContent: OnUnshareContent(message.UnshareContent); return true;
                case UIAction.InnerOneofCase.ConnectPeer: OnConnectPeer(message.ConnectPeer); return true;
                default: throw new Exception("fatal: received invalid UI Action");
            }
        }
        public static void Main()
        {
            while (UIActionHandle()) { }
        }
    }
}
