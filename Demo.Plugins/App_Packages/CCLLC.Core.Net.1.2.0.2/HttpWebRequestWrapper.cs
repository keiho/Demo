using System;
using System.Globalization;
using System.IO;
using System.Net;

namespace CCLLC.Core.Net
{   
    public class HttpWebRequestWrapper : IWebRequest
    {
        private const string ContentEncodingHeader = "Content-Encoding";
        private const int DefaultTimeoutInSeconds = 30;

        protected Uri Address { get; private set; }
       
        public ICredentials Credentials { get; set; }

        public WebHeaderCollection Headers { get; set; }

        public TimeSpan Timeout { get; set; }

        public HttpWebRequestWrapper(IAPIEndpoint endpoint) : this(endpoint.ToUri()) { }

        public HttpWebRequestWrapper(Uri address) : base()
        {
            Address = address ?? throw new ArgumentNullException("address cannot be null.");

            this.Headers = new WebHeaderCollection();
            this.Timeout = TimeSpan.FromSeconds(DefaultTimeoutInSeconds);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Address = null;
            this.Credentials = null;
            this.Headers = null;
        }

        public virtual IWebResponse Delete()
        {
            try
            {
                var webRequest = (System.Net.HttpWebRequest)WebRequest.Create(Address);
                webRequest.Method = "DELETE";

                InitializeWebRequest(webRequest);

                return GetResponse(webRequest);

            }
            catch (WebException ex)
            {
                string str = string.Empty;
                if (ex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        str = reader.ReadToEnd();
                    }
                    ex.Response.Close();
                }

                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Exception("Web Request Timeout occurred.", ex);
                }

                throw new Exception(String.Format(CultureInfo.InvariantCulture,
                    "A Web exception occurred while attempting to issue the request. {0}: {1}",
                    ex.Message, str), ex);
            }
        }

        public virtual IWebResponse Get()
        {  
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(Address);
                webRequest.Method = "GET";

                InitializeWebRequest(webRequest);

                return GetResponse(webRequest);

            }
            catch (WebException ex)
            {
                string str = string.Empty;
                if (ex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        str = reader.ReadToEnd();
                    }
                    ex.Response.Close();
                }

                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Exception("Web Request Timeout occurred.", ex);
                }

                throw new Exception(String.Format(CultureInfo.InvariantCulture,
                    "A Web exception occurred while attempting to issue the request. {0}: {1}",
                    ex.Message, str), ex);
            }            
        }
        
        public virtual IWebResponse Post(byte[] data, string contentType = null, string contentEncoding = null)
        { 
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(Address);
                webRequest.Method = "POST";

                InitializeWebRequest(webRequest, contentType, contentEncoding);
                WriteDataToRequestBody(webRequest, data);

                return GetResponse(webRequest);
            }
            catch (WebException ex)
            {
                string str = string.Empty;
                if (ex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        str = reader.ReadToEnd();
                    }
                    ex.Response.Close();
                }
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Exception("Web Request Timeout occurred.", ex);
                }
                throw new Exception(String.Format(CultureInfo.InvariantCulture,
                    "A Web exception occurred while attempting to issue the request. {0}: {1}",
                    ex.Message, str), ex);
            }            

        }

        public virtual IWebResponse Put(string data, string contentType = null)
        {      
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(Address);
                webRequest.Method = "PUT";

                InitializeWebRequest(webRequest);
                WriteDataToRequestBody(webRequest, data);
                
                return GetResponse(webRequest);                
            }
            catch (WebException ex)
            {
                string str = string.Empty;
                if (ex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        str = reader.ReadToEnd();
                    }
                    ex.Response.Close();
                }
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Exception("Web Request Timeout occurred.", ex);
                }
                throw new Exception(String.Format(CultureInfo.InvariantCulture,
                    "A Web exception occurred while attempting to issue the request. {0}: {1}",
                    ex.Message, str), ex);
            }          

        }

        public virtual IWebResponse Put(byte[] data, string contentType = null)
        {
            try
            {
                var webRequest = (HttpWebRequest)WebRequest.Create(Address);
                webRequest.Method = "PUT";

                InitializeWebRequest(webRequest, contentType);
                WriteDataToRequestBody(webRequest, data);

                return GetResponse(webRequest);
            }
            catch (WebException ex)
            {
                string str = string.Empty;
                if (ex.Response != null)
                {
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                    {
                        str = reader.ReadToEnd();
                    }
                    ex.Response.Close();
                }
                if (ex.Status == WebExceptionStatus.Timeout)
                {
                    throw new Exception("Web Request Timeout occurred.", ex);
                }
                throw new Exception(String.Format(CultureInfo.InvariantCulture,
                    "A Web exception occurred while attempting to issue the request. {0}: {1}",
                    ex.Message, str), ex);
            }

        }

        private void InitializeWebRequest(HttpWebRequest webRequest, string contentType = null, string contentEncoding = null)
        {
            SetHeaders(webRequest);

            if (this.Credentials != null)
            {
                webRequest.Credentials = this.Credentials;
            }

            if (contentType != null)
            {
                webRequest.ContentType = contentType;
            }

            if (!string.IsNullOrEmpty(contentEncoding))
            {
                webRequest.Headers[ContentEncodingHeader] = contentEncoding;
            }            

            webRequest.Timeout = (int)this.Timeout.TotalMilliseconds;
        }

        private void SetHeaders(HttpWebRequest webRequest)
        {
            foreach (var key in this.Headers.AllKeys)
            {
                switch (key)
                {
                    case "Date":
                        webRequest.Date = DateTime.Parse(this.Headers.Get(key));
                        break;
                    case "Content-Length":
                        webRequest.ContentLength = int.Parse(this.Headers.Get(key));
                        break;
                    case "Content-Type":
                        webRequest.ContentType = this.Headers.Get(key);
                        break;
                    default:
                        webRequest.Headers.Add(key, this.Headers.Get(key));
                        break;
                }
            }
        }
    
        private void WriteDataToRequestBody(HttpWebRequest webRequest, byte[] data)
        {
            if (data != null && data.Length > 0)
            {
                using (Stream requestStream = webRequest.GetRequestStream())
                {
                    requestStream.Write(data, 0, data.Length);
                }
            }
        }

        private void WriteDataToRequestBody(HttpWebRequest webRequest, string data)
        {
            if (data != null && data.Length > 0)
            {
                using (var streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    streamWriter.Write(data);
                }
            }
        }

        private IWebResponse GetResponse(HttpWebRequest webRequest)
        {
            using (HttpWebResponse response = webRequest.GetResponse() as HttpWebResponse)
            {
                var webResponse = new HttpWebResponseWrapper(response);
                return webResponse;
            }
        }
    }
}
