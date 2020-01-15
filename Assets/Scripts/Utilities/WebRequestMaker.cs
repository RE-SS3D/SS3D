using System.IO;
using System.Net;
using System.Text;

namespace Utilities
{   
    /// <summary>
    /// Class to simplify making requests to an API.
    /// </summary>
    public class WebRequestMaker
    {
        public enum PostMethod
        {
            POST,
            DELETE
        }
        
        public static string Get(string uri, string authorizationToken = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            
            if (authorizationToken != null)
            {
                request.Headers.Add("authorization", $"Bearer {authorizationToken}");
            }

            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using(Stream stream = response.GetResponseStream())
            using(StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        
        public static string Post(string uri, string data, PostMethod method = PostMethod.POST, string authorizationToken = null)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/json";
            request.Method = method.ToString();
            
            if (authorizationToken != null)
            {
                request.Headers.Add("authorization", $"Bearer {authorizationToken}");
            }

            using(Stream requestBody = request.GetRequestStream())
            {
                requestBody.Write(dataBytes, 0, dataBytes.Length);
            }

            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using(Stream stream = response.GetResponseStream())
            using(StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}