

namespace CCLLC.Core.Net
{
    using System.IO;
    using System.Net;
    using System.Text;

    public class HttpWebResponseWrapper : IWebResponse
    {
        public string Content { get; }
        public byte[] Bytes { get; }
        public WebHeaderCollection Headers { get;  }
        public int StatusCode { get;  }
        public string StatusDescription { get;  }
        public bool Success { get;  }

        public HttpWebResponseWrapper(HttpWebResponse response)
        {
            if (response != null)
            {
                this.Headers = new WebHeaderCollection();
                foreach (var key in response.Headers.AllKeys)
                {
                    this.Headers.Add(key, response.Headers[key]);
                }

                this.StatusCode = (int)response.StatusCode;
                this.StatusDescription = response.StatusDescription;
                this.Success = (this.StatusCode >= 200 && this.StatusCode < 300);

               
                using (var stream = response.GetResponseStream())
                {
                    var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    var bytes = ms.ToArray();
                    this.Bytes = bytes;
                    this.Content = new UTF8Encoding().GetString(bytes);
                }               

            }
        }
    }
}
