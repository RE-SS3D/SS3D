using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace SS3D.Launcher
{
    /// <summary>Parses what players paste: <c>host</c>, <c>host:port</c>, <c>[ipv6]:port</c> or a bare IPv6 address.</summary>
    public static class MainMenuAddress
    {
        public static bool TryParse(string input, ushort defaultPort, out string host, out ushort port, out string error)
        {
            host = "";
            port = defaultPort;
            error = "";
            string text = (input ?? "").Trim();
            if (text.Length == 0)
            {
                error = "Paste a server address.";
                return false;
            }

            if (text.Contains("://") || text.Contains("/") || text.Contains(" "))
            {
                error = "That doesn't look like a server address. Use address or address:port.";
                return false;
            }

            string portText = null;
            if (text.StartsWith("["))
            {
                int close = text.IndexOf(']');
                if (close < 0)
                {
                    error = "Missing ] after the IPv6 address.";
                    return false;
                }

                host = text.Substring(1, close - 1);
                string rest = text.Substring(close + 1);
                if (rest.Length > 0)
                {
                    if (!rest.StartsWith(":"))
                    {
                        error = "Use [address]:port for IPv6 addresses.";
                        return false;
                    }

                    portText = rest.Substring(1);
                }
            }
            else if (text.IndexOf(':') != text.LastIndexOf(':'))
            {
                // Several colons and no brackets: a bare IPv6 address without a port.
                host = text;
            }
            else
            {
                int colon = text.IndexOf(':');
                host = colon < 0 ? text : text.Substring(0, colon);
                portText = colon < 0 ? null : text.Substring(colon + 1);
            }

            if (host.Length == 0)
            {
                error = "Missing the address before the port.";
                return false;
            }

            if (host.Contains(":") && (!IPAddress.TryParse(host, out IPAddress v6) || v6.AddressFamily != AddressFamily.InterNetworkV6))
            {
                error = "That IPv6 address isn't valid.";
                return false;
            }

            if (portText != null && !TryParsePort(portText, out port))
            {
                error = "The port must be a number from 1 to 65535.";
                return false;
            }

            return true;
        }

        public static bool TryParsePort(string text, out ushort port)
        {
            port = 0;
            return int.TryParse((text ?? "").Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value)
                && value is >= 1 and <= ushort.MaxValue
                && (port = (ushort)value) > 0;
        }

        public static string Format(string host, ushort port) =>
            host.Contains(":") ? $"[{host}]:{port}" : $"{host}:{port}";
    }
}
