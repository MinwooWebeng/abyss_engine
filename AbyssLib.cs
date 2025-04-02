using AbyssCLI.Aml;
using AbyssCLI.Tool;
using Google.Protobuf;
using Microsoft.VisualBasic;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using static AbyssCLI.ABI.UIAction.Types;

#nullable enable
namespace AbyssCLI
{
    static internal class AbyssLib
    {
        static readonly int _i = Init();
        public enum ErrorCode: int
        {
            SUCCESS = 0,
            ERROR = -1, //also EOF
            INVALID_ARGUMENTS = -2,
            BUFFER_OVERFLOW = -3,
            INVALID_HANDLE = -99,
        }
        static public string GetVersion()
        {
            unsafe
            {
                [DllImport("abyssnet.dll")]
                static extern int GetVersion(byte* buf, int buflen);

                fixed (byte* pBytes = new byte[16])
                {
                    int len = GetVersion(pBytes, 16);
                    if (len < 0)
                    {
                        return "error";
                    }
                    return System.Text.Encoding.UTF8.GetString(pBytes, len);
                }
            }
        }
        static public int Init()
        {
            [DllImport("abyssnet.dll")]
            static extern int Init();
            return Init();
        }
        static private void CloseAbyssHandle(IntPtr handle)
        {
            [DllImport("abyssnet.dll")]
            static extern void CloseHandle(IntPtr handle);
            CloseHandle(handle);
        }
        public class DLLError(IntPtr _error_handle)
        {
            private readonly IntPtr error_handle = _error_handle;

            public override string ToString()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int GetErrorBodyLength(IntPtr err_handle);

                    [DllImport("abyssnet.dll")]
                    static extern int GetErrorBody(IntPtr err_handle, byte* buf, int buflen);

                    var msg_len = GetErrorBodyLength(error_handle);
                    var buf = new byte[msg_len];
                    fixed (byte* dBytes = buf)
                    {
                        var len = GetErrorBody(error_handle, dBytes, buf.Length);
                        if (len != buf.Length)
                        {
                            throw new Exception("AbyssDLL: buffer write failure");
                        }
                        return Encoding.UTF8.GetString(buf);
                    }
                }
            }
            ~DLLError() => CloseAbyssHandle(error_handle);
        }
        static public DLLError GetError()
        {
            [DllImport("abyssnet.dll")]
            static extern IntPtr PopErrorQueue();
            return (new DLLError(PopErrorQueue()));
        }
        public class SimplePathResolver(IntPtr _handle)
        {
            public readonly IntPtr handle = _handle;
            public ErrorCode SetMapping(string path, byte[] world_id)
            {
                byte[] path_bytes;
                try
                {
                    path_bytes = Encoding.ASCII.GetBytes(path);
                }
                catch
                {
                    return ErrorCode.INVALID_ARGUMENTS;
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int SimplePathResolver_SetMapping(IntPtr h, byte* path_ptr, int path_len,byte* world_ID);
                    fixed (byte* path_ptr = path_bytes)
                    {
                        fixed (byte *world_id_ptr = world_id)
                        {
                            return (ErrorCode)SimplePathResolver_SetMapping(handle, path_ptr, path_bytes.Length, world_id_ptr);
                        }
                    }
                }
            }
            public ErrorCode DeleteMapping(string path)
            {
                byte[] path_bytes;
                try
                {
                    path_bytes = Encoding.ASCII.GetBytes(path);
                }
                catch
                {
                    return ErrorCode.INVALID_ARGUMENTS;
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int SimplePathResolver_DeleteMapping(IntPtr h, byte* path_ptr, int path_len);
                    fixed (byte* path_ptr = path_bytes)
                    {
                        return (ErrorCode)SimplePathResolver_DeleteMapping(handle, path_ptr, path_bytes.Length);
                    }
                }
            }
            ~SimplePathResolver() => CloseAbyssHandle(handle);
        }
        static public SimplePathResolver NewSimplePathResolver()
        {
            [DllImport("abyssnet.dll")]
            static extern IntPtr NewSimplePathResolver();
            return new SimplePathResolver(NewSimplePathResolver());
        }
        public class Host
        {
            public Host(IntPtr _handle)
            {
                if (handle == IntPtr.Zero)
                {
                    throw new Exception("invalid host handle");
                }
                handle = _handle;

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int Host_GetLocalAbyssURL(IntPtr h, byte* buf, int buflen);

                    [DllImport("abyssnet.dll")]
                    static extern int Host_GetCertificates(IntPtr h, byte* root_cert_buf, int* root_cert_len, byte* hs_key_cert_buf, int* hs_key_cert_len);

                    fixed (byte* pBytes = new byte[256])
                    {
                        int len = Host_GetLocalAbyssURL(handle, pBytes, 256);
                        if (!AbyssURLParser.TryParse(len <= 0 ? "" : System.Text.Encoding.ASCII.GetString(pBytes, len), out local_aurl))
                        {
                            throw new Exception("failed to parse local host AURL");
                        }
                    }

                    int root_cert_len;
                    int hs_key_cert_len;
                    _ = Host_GetCertificates(handle, (byte*)0, &root_cert_len, (byte*)0, &hs_key_cert_len);

                    root_certificate = new byte[root_cert_len];
                    handshake_key_certificate = new byte[hs_key_cert_len];

                    fixed (byte* rbuf = root_certificate)
                    {
                        fixed (byte* kbuf = handshake_key_certificate)
                        {
                            if (Host_GetCertificates(handle, rbuf, &root_cert_len, kbuf, &hs_key_cert_len) != 0)
                            {
                                throw new Exception("failed to receive local host certificates");
                            }
                        }
                    }
                }
            }
            private readonly IntPtr handle;
            public readonly AbyssURL local_aurl;
            public readonly byte[] root_certificate;
            public readonly byte[] handshake_key_certificate;
            public bool IsValid() { return handle != IntPtr.Zero; }
            public ErrorCode AppendKnownPeer(byte[] root_cert, byte[] hs_key_cert)
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int Host_AppendKnownPeer(IntPtr h, byte* root_cert_buf, int root_cert_len, byte* hs_key_cert_buf, int hs_key_cert_len);

                    fixed (byte* rbuf = root_cert)
                    {
                        fixed (byte* kbuf = hs_key_cert)
                        {
                            return (ErrorCode)Host_AppendKnownPeer(handle, rbuf, root_cert.Length, kbuf, hs_key_cert.Length);
                        }
                    }
                }
            }
            public ErrorCode OpenOutboundConnection(string aurl)
            {
                byte[] aurl_bytes;
                try
                {
                    aurl_bytes = Encoding.ASCII.GetBytes(aurl);
                }
                catch
                {
                    return ErrorCode.INVALID_ARGUMENTS;
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int Host_OpenOutboundConnection(IntPtr h, byte* aurl_ptr, int aurl_len);

                    fixed (byte* aurl_ptr = aurl_bytes)
                    {
                        return (ErrorCode)Host_OpenOutboundConnection(handle, aurl_ptr, aurl_bytes.Length);
                    }
                }
            }
            public World OpenWorld(string url)
            {
                byte[] url_bytes;
                try
                {
                    url_bytes = Encoding.ASCII.GetBytes(url);
                }
                catch
                {
                    return new World(IntPtr.Zero);
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr Host_OpenWorld(IntPtr h, byte* url_ptr, int url_len);

                    fixed (byte* url_ptr = url_bytes)
                    {
                        var world_handle = Host_OpenWorld(handle, url_ptr, url_bytes.Length);
                        return new World(world_handle);
                    }
                }
            }
            public World JoinWorld(string aurl)
            {
                byte[] aurl_bytes;
                try
                {
                    aurl_bytes = Encoding.ASCII.GetBytes(aurl);
                }
                catch
                {
                    return new World(IntPtr.Zero);
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr Host_JoinWorld(IntPtr h, byte* url_ptr, int url_len, int timeout_ms);

                    fixed (byte* aurl_ptr = aurl_bytes)
                    {
                        var world_handle = Host_JoinWorld(handle, aurl_ptr, aurl_bytes.Length, 1000);
                        return new World(world_handle);
                    }
                }
            }
            ~Host() => CloseAbyssHandle(handle);
        }
        static public Host OpenAbyssHost(byte[] root_priv_key_pem, SimplePathResolver path_resolver)
        {
            unsafe
            {
                [DllImport("abyssnet.dll")]
                static extern IntPtr NewHost(byte* root_priv_key_pem_ptr, int root_priv_key_pem_len, IntPtr h_path_resolver);

                fixed (byte* key_ptr = root_priv_key_pem)
                {
                    return new Host(NewHost(key_ptr, root_priv_key_pem.Length, path_resolver.handle));
                }
            }
        }
        public class World
        {
            public World(IntPtr _handle)
            {
                handle = _handle;

                if (handle == IntPtr.Zero)
                {
                    world_id = [];
                    return;
                }

                world_id = new byte[16];
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int World_GetSessionID(IntPtr h, byte* world_ID_out);

                    fixed (byte* buf_ptr = world_id)
                    {
                        _ = World_GetSessionID(handle, buf_ptr);
                    }
                }
            }
            private readonly IntPtr handle;
            public readonly byte[] world_id;
            public bool IsValid()
            {
                return handle != IntPtr.Zero;
            }
            public dynamic WaitForEvent()
            {
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern IntPtr World_WaitEvent(IntPtr h, int* event_type_out);

                    int t;
                    IntPtr ret_handle = World_WaitEvent(handle, &t);

                    return t switch
                    {
                        1 => new WorldMemberRequest(ret_handle),
                        2 => new WorldMember(ret_handle),
                        3 => new MemberObjectAppend(ret_handle),
                        4 => new MemberObjectDelete(ret_handle),
                        5 => new WorldMemberLeave(ret_handle),
                        _ => 0,
                    };
                }
            }
            ~World() => CloseAbyssHandle(handle);
        }
        public class WorldMemberRequest
        {
            public WorldMemberRequest(IntPtr _handle)
            {
                handle = _handle;

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerRequest_GetHash(IntPtr h, byte* buf, int buflen);

                    fixed (byte* buf = new byte[128])
                    {
                        int res_len = WorldPeerRequest_GetHash(handle, buf, 128);
                        peer_hash = res_len <= 0 ? "" : Encoding.ASCII.GetString(buf, res_len);
                    }
                }
            }
            private readonly IntPtr handle;
            public readonly string peer_hash;
            public ErrorCode Accept()
            {
                [DllImport("abyssnet.dll")]
                static extern int WorldPeerRequest_Accept(IntPtr h);

                return (ErrorCode)WorldPeerRequest_Accept(handle);
            }
            public ErrorCode Decline(int code, string msg)
            {
                byte[] msg_bytes;
                try
                {
                    msg_bytes = Encoding.ASCII.GetBytes(msg);
                }
                catch
                {
                    return ErrorCode.INVALID_ARGUMENTS;
                }

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerRequest_Decline(IntPtr h, int code, byte* msg, int msglen);

                    fixed(byte* msg_ptr = msg_bytes)
                    {
                        return (ErrorCode)WorldPeerRequest_Decline(handle, code, msg_ptr, msg_bytes.Length);
                    }
                }
            }
            ~WorldMemberRequest() => CloseAbyssHandle(handle);
        }
        public class ObjectInfoFormat
        {
            public required string ID { get; set; }
            public required string Addr { get; set; }
        }
        private static string BytesToHex(byte[] input)
        {
            char[] result = new char[input.Length * 2];
            for (int i = 0; i < input.Length; i++)
            {
                byte b = input[i];
                result[i * 2] = (char)(b >> 4 <= 9 ? '0' + (b >> 4) : 'A' + (b >> 4) - 10);
                result[i * 2 + 1] = (char)((b & 0x0F) <= 9 ? '0' + (b & 0x0F) : 'A' + (b & 0x0F) - 10);
            }
            return new string(result);
        }
        private static int HexCharToNibble(char c)
        {
            if (c >= '0' && c <= '9')
                return c - '0';
            else if (c >= 'A' && c <= 'F')
                return c - 'A' + 10;
            else if (c >= 'a' && c <= 'f')
                return c - 'a' + 10;
            else
                throw new ArgumentException($"Invalid hex character: {c}");
        }
        private static byte[] HexToBytes(string hex)
        {
            byte[] result = new byte[hex.Length / 2];

            for (int i = 0; i < result.Length; i++)
            {
                int high = HexCharToNibble(hex[i * 2]);
                int low = HexCharToNibble(hex[i * 2 + 1]);

                result[i] = (byte)((high << 4) | low);
            }

            return result;
        }
        public class WorldMember
        {
            public WorldMember(IntPtr _handle)
            {
                handle = _handle;

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeer_GetHash(IntPtr h, byte* buf, int buflen);

                    fixed (byte* buf = new byte[128])
                    {
                        int len = WorldPeer_GetHash(handle, buf, 128);
                        hash = len < 0 ? "" : System.Text.Encoding.ASCII.GetString(buf, len);
                    }
                }
            }
            public ErrorCode AppendObjects(Tuple<Guid, string>[] objects_info)
            {
                var objinfo_marshalled = objects_info.Select(x => new ObjectInfoFormat { ID = BytesToHex(x.Item1.ToByteArray()), Addr = x.Item2 }).ToArray();
                var data = JsonSerializer.Serialize(objinfo_marshalled);
                byte[] data_bytes;
                try
                {
                    data_bytes = Encoding.ASCII.GetBytes(data);
                }
                catch
                {
                    return ErrorCode.INVALID_ARGUMENTS;
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeer_AppendObjects(IntPtr h, byte* json_ptr, int json_len);

                    fixed(byte* data_ptr = data_bytes)
                    {
                        return (ErrorCode)WorldPeer_AppendObjects(handle, data_ptr, data_bytes.Length);
                    }
                }
            }
            public ErrorCode DeleteObjects(Guid[] object_ids)
            {
                var objid_marshalled = object_ids.Select(x => BytesToHex(x.ToByteArray()));
                var data = JsonSerializer.Serialize(objid_marshalled);
                byte[] data_bytes;
                try
                {
                    data_bytes = Encoding.ASCII.GetBytes(data);
                }
                catch
                {
                    return ErrorCode.INVALID_ARGUMENTS;
                }
                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeer_DeleteObjects(IntPtr h, byte* json_ptr, int json_len);

                    fixed(byte* data_ptr = data_bytes)
                    {
                        return (ErrorCode)WorldPeer_DeleteObjects(handle, data_ptr, data_bytes.Length);
                    }
                }
            }
            private readonly IntPtr handle;
            public readonly string hash;
            ~WorldMember() => CloseAbyssHandle(handle);
        }
        public class MemberObjectAppend
        {
            public MemberObjectAppend(IntPtr _handle)
            {
                handle = _handle;

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerObjectAppend_GetHead(IntPtr h, byte* peer_hash_out, int* body_len);

                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerObjectAppend_GetBody(IntPtr h, byte* buf, int buflen);

                    int body_len = 0;
                    fixed (byte* buf = new byte[128])
                    {
                        int hash_len = WorldPeerObjectAppend_GetHead(handle, buf, &body_len);
                        peer_hash = hash_len < 0 ? "" : System.Text.Encoding.ASCII.GetString(buf, hash_len);
                    }
                    if (body_len <= 0) {
                        objects = [];
                        return; 
                    }

                    ObjectInfoFormat[]? infos;
                    fixed (byte* buf = new byte[body_len])
                    {
                        int res_len = WorldPeerObjectAppend_GetBody(handle, buf, body_len);
                        if (res_len != body_len) {
                            objects = [];
                            return;
                        }
                        infos = JsonSerializer.Deserialize<ObjectInfoFormat[]>(System.Text.Encoding.ASCII.GetString(buf, res_len));
                    }

                    objects = infos == null ? [] : infos.Select(x => Tuple.Create(new Guid(HexToBytes(x.ID)), x.Addr)).ToArray();
                }
            }
            private readonly IntPtr handle;
            public readonly string peer_hash;
            public readonly Tuple<Guid, string>[] objects;
            ~MemberObjectAppend() => CloseAbyssHandle(handle);
        }
        public class MemberObjectDelete
        {
            public MemberObjectDelete(IntPtr _handle)
            {
                handle = _handle;

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerObjectDelete_GetHead(IntPtr h, byte* peer_hash_out, int* body_len);

                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerObjectDelete_GetBody(IntPtr h, byte* buf, int buflen);

                    int body_len = 0;
                    fixed (byte* buf = new byte[128])
                    {
                        int hash_len = WorldPeerObjectDelete_GetHead(handle, buf, &body_len);
                        peer_hash = hash_len < 0 ? "" : System.Text.Encoding.ASCII.GetString(buf, hash_len);
                    }
                    if (body_len <= 0)
                    {
                        object_ids = [];
                        return;
                    }

                    string[]? infos;
                    fixed (byte* buf = new byte[body_len])
                    {
                        int res_len = WorldPeerObjectDelete_GetBody(handle, buf, body_len);
                        if (res_len != body_len)
                        {
                            object_ids = [];
                            return;
                        }
                        infos = JsonSerializer.Deserialize<string[]>(System.Text.Encoding.ASCII.GetString(buf, res_len));
                    }

                    object_ids = infos == null ? [] : infos.Select(x => new Guid(HexToBytes(x))).ToArray();
                }
            }
            private readonly IntPtr handle;
            public readonly string peer_hash;
            public readonly Guid[] object_ids;
            ~MemberObjectDelete() => CloseAbyssHandle(handle);
        }
        public class WorldMemberLeave
        {
            public WorldMemberLeave(IntPtr _handle)
            {
                handle = _handle;

                unsafe
                {
                    [DllImport("abyssnet.dll")]
                    static extern int WorldPeerLeave_GetHash(IntPtr h, byte* buf, int buflen);

                    fixed (byte* buf = new byte[128])
                    {
                        int len = WorldPeerLeave_GetHash(handle, buf, 128);
                        peer_hash = len < 0 ? "" : System.Text.Encoding.ASCII.GetString(buf, len);
                    }
                }
            }
            private readonly IntPtr handle;
            public readonly string peer_hash;
            ~WorldMemberLeave() => CloseAbyssHandle(handle);
        }
    }
}
