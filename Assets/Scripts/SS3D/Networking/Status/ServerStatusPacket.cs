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
    /// <para>
    /// Two request forms exist. The original (<c>SS3D-STATUS?1</c>) carries nothing, so its reply
    /// cannot be matched to a particular request. The latency probe (<c>SS3D-STATUS?2</c> + a 4-byte
    /// nonce) is answered by <c>SS3D-STATUS!2</c> + the same nonce + the status fields, so a client
    /// can time each probe exactly. Servers answer both; clients fall back to the original for
    /// servers that only know it.
    /// </para>
    /// </summary>
    public static class ServerStatusPacket
    {
        private const int MaxText = 64;
        private static readonly byte[] RequestBytes = Encoding.ASCII.GetBytes("SS3D-STATUS?1");
        private static readonly byte[] ReplyMagic = Encoding.ASCII.GetBytes("SS3D-STATUS!1");
        private static readonly byte[] ProbeRequestMagic = Encoding.ASCII.GetBytes("SS3D-STATUS?2");
        private static readonly byte[] ProbeReplyMagic = Encoding.ASCII.GetBytes("SS3D-STATUS!2");
        private const int NonceSize = sizeof(uint);

        /// <summary>The original, nonce-less request.</summary>
        public static byte[] Request() => (byte[])RequestBytes.Clone();

        /// <summary>A latency probe; the server echoes <paramref name="nonce"/> in its reply.</summary>
        public static byte[] Request(uint nonce)
        {
            byte[] data = new byte[ProbeRequestMagic.Length + NonceSize];
            ProbeRequestMagic.CopyTo(data, 0);
            WriteNonce(data, ProbeRequestMagic.Length, nonce);
            return data;
        }

        /// <summary>True for the original, nonce-less request only.</summary>
        public static bool IsRequest(byte[] data) => data != null && data.AsSpan().SequenceEqual(RequestBytes);

        /// <summary>True for a latency probe, giving the nonce to echo.</summary>
        public static bool TryReadRequestNonce(byte[] data, out uint nonce)
        {
            nonce = 0;
            if (data == null || data.Length != ProbeRequestMagic.Length + NonceSize
                || !data.AsSpan(0, ProbeRequestMagic.Length).SequenceEqual(ProbeRequestMagic))
            {
                return false;
            }

            nonce = BitConverter.ToUInt32(data, ProbeRequestMagic.Length);
            return true;
        }

        /// <summary>Reply to the original request (no nonce).</summary>
        public static byte[] EncodeReply(in ServerStatus status)
        {
            using MemoryStream stream = new();
            stream.Write(ReplyMagic, 0, ReplyMagic.Length);
            WriteFields(stream, status);
            return stream.ToArray();
        }

        /// <summary>Reply to a latency probe. To answer many probes from one cache, encode once and <see cref="WithNonce"/> each.</summary>
        public static byte[] EncodeReply(in ServerStatus status, uint nonce)
        {
            using MemoryStream stream = new();
            stream.Write(ProbeReplyMagic, 0, ProbeReplyMagic.Length);
            byte[] nonceBytes = new byte[NonceSize];
            WriteNonce(nonceBytes, 0, nonce);
            stream.Write(nonceBytes, 0, nonceBytes.Length);
            WriteFields(stream, status);
            return stream.ToArray();
        }

        /// <summary>Copy of a probe reply from <see cref="EncodeReply(in ServerStatus, uint)"/> carrying a different nonce.</summary>
        public static byte[] WithNonce(byte[] probeReply, uint nonce)
        {
            byte[] copy = (byte[])probeReply.Clone();
            WriteNonce(copy, ProbeReplyMagic.Length, nonce);
            return copy;
        }

        public static bool TryDecodeReply(byte[] data, out ServerStatus status) =>
            TryDecodeReply(data, out status, out _, out _);

        /// <param name="hasNonce">False for a reply to the original request.</param>
        public static bool TryDecodeReply(byte[] data, out ServerStatus status, out bool hasNonce, out uint nonce)
        {
            status = default;
            hasNonce = false;
            nonce = 0;
            int start;
            if (StartsWith(data, ReplyMagic))
            {
                start = ReplyMagic.Length;
            }
            else if (StartsWith(data, ProbeReplyMagic) && data.Length >= ProbeReplyMagic.Length + NonceSize)
            {
                hasNonce = true;
                nonce = BitConverter.ToUInt32(data, ProbeReplyMagic.Length);
                start = ProbeReplyMagic.Length + NonceSize;
            }
            else
            {
                return false;
            }

            try
            {
                using MemoryStream stream = new(data, start, data.Length - start);
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

        private static void WriteFields(Stream stream, in ServerStatus status)
        {
            using BinaryWriter writer = new(stream, Encoding.UTF8, true);
            writer.Write(status.Protocol);
            writer.Write(Clip(status.Version));
            writer.Write(Clip(status.Name));
            writer.Write(status.Players);
            writer.Write(status.PlayerCap);
        }

        private static bool StartsWith(byte[] data, byte[] magic) =>
            data != null && data.Length >= magic.Length && data.AsSpan(0, magic.Length).SequenceEqual(magic);

        private static void WriteNonce(byte[] data, int offset, uint nonce) =>
            BitConverter.GetBytes(nonce).CopyTo(data, offset);

        private static string Clip(string text)
        {
            text ??= "";
            return text.Length > MaxText ? text.Substring(0, MaxText) : text;
        }
    }
}
