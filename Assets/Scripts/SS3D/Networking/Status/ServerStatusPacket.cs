using System;
using System.IO;
using System.Text;

namespace SS3D.Networking.Status
{
    /// <summary>What a server reports to the main menu before anyone joins.</summary>
    public readonly struct ServerStatus
    {
        public readonly int Protocol;
        public readonly string Version;
        public readonly string Name;
        public readonly int Players;
        public readonly int PlayerCap;

        public ServerStatus(int protocol, string version, string name, int players, int playerCap)
        {
            Protocol = protocol;
            Version = version ?? "";
            Name = name ?? "";
            Players = players;
            PlayerCap = playerCap;
        }

        public bool IsFull => PlayerCap > 0 && Players >= PlayerCap;
    }

    /// <summary>
    /// Wire format of the unconnected status query answered on the game port (see the SS3D patch in
    /// Tugboat's <c>ServerSocket.UnconnectedRequestHandler</c>). Independent of
    /// <see cref="ProtocolVersion"/>: the reply carries the server's protocol so a client of any
    /// version can tell whether it may join. Servers that predate the query never answer.
    /// </summary>
    public static class ServerStatusPacket
    {
        private const int MaxText = 64;
        private static readonly byte[] RequestBytes = Encoding.ASCII.GetBytes("SS3D-STATUS?1");
        private static readonly byte[] ReplyMagic = Encoding.ASCII.GetBytes("SS3D-STATUS!1");

        public static byte[] Request() => (byte[])RequestBytes.Clone();

        public static bool IsRequest(byte[] data) => data != null && data.AsSpan().SequenceEqual(RequestBytes);

        public static byte[] EncodeReply(in ServerStatus status)
        {
            using MemoryStream stream = new();
            stream.Write(ReplyMagic, 0, ReplyMagic.Length);
            using (BinaryWriter writer = new(stream, Encoding.UTF8, true))
            {
                writer.Write(status.Protocol);
                writer.Write(Clip(status.Version));
                writer.Write(Clip(status.Name));
                writer.Write(status.Players);
                writer.Write(status.PlayerCap);
            }

            return stream.ToArray();
        }

        public static bool TryDecodeReply(byte[] data, out ServerStatus status)
        {
            status = default;
            if (data == null || data.Length < ReplyMagic.Length
                || !data.AsSpan(0, ReplyMagic.Length).SequenceEqual(ReplyMagic))
            {
                return false;
            }

            try
            {
                using MemoryStream stream = new(data, ReplyMagic.Length, data.Length - ReplyMagic.Length);
                using BinaryReader reader = new(stream, Encoding.UTF8);
                int protocol = reader.ReadInt32();
                string version = Clip(reader.ReadString());
                string name = Clip(reader.ReadString());
                int players = reader.ReadInt32();
                int cap = reader.ReadInt32();
                status = new ServerStatus(protocol, version, name, Math.Max(0, players), Math.Max(0, cap));
                return true;
            }
            catch (Exception exception) when (exception is EndOfStreamException or IOException or ArgumentException)
            {
                return false;
            }
        }

        private static string Clip(string text)
        {
            text ??= "";
            return text.Length > MaxText ? text.Substring(0, MaxText) : text;
        }
    }
}
