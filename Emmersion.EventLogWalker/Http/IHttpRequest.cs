using System.Net.Http;

namespace Emmersion.EventLogWalker.Http
{
    internal interface IHttpRequest
    {
        string Url { get; set; }
        HttpHeaders Headers { get; set; }

        bool HasContent();
        HttpContent GetContent();
    }
}
