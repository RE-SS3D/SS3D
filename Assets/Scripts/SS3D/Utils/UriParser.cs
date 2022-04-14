using System;
using System.Net;

namespace SS3D.Utils
{
    /// <summary>
    /// Process an IP from string to Uri
    /// </summary>
    public static class UriParser
    {
        /// <summary>
        /// This handles getting the IP address from a string, it needs to be transformed in a Uri in order to work with the NetworkManager 
        /// </summary>
        /// <param name="ip">SS3D server ip</param>
        /// <returns>SS3D server Uri</returns>
        public static Uri TryParseIpAddress(string ip)
        {
            UriBuilder uriBuilder = new UriBuilder
            {
                Scheme = "kcp",
                Host = IPAddress.TryParse(ip, out IPAddress address) ? address.ToString() : "localhost"
            };

            Uri uri = new Uri(uriBuilder.ToString(), UriKind.Absolute);
            return uri;
        }
    }
}