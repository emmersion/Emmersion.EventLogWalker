using System.Net.Http;
using System.Text;

namespace Emmersion.EventLogWalker.Http
{
    public class HttpRequest : IHttpRequest
    {
        public HttpRequest()
        {
            Headers = new HttpHeaders();
        }

        public string Url { get; set; }
        public HttpHeaders Headers { get; set; }
        public string Body { get; set; }

        public bool HasContent() => Body != null;

        public HttpContent GetContent() => new ByteArrayContent(Encoding.UTF8.GetBytes(Body));
    }
}
