using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Emmersion.EventLogWalker.Http
{
    public interface IHttpClient
    {
        Task<HttpResponse> ExecutePostAsync(IHttpRequest request);
    }

    public class HttpClient : IHttpClient, IDisposable
    {
        private readonly System.Net.Http.HttpClient client;

        public HttpClient()
        {
            var handler = new HttpClientHandler {UseCookies = false};
            client = new System.Net.Http.HttpClient(handler);
        }

        public void Dispose()
        {
            client?.Dispose();
        }

        public async Task<HttpResponse> ExecutePostAsync(IHttpRequest request)
        {
            var requestMessage = BuildRequestMessage(request);
            var response = await client.SendAsync(requestMessage).ConfigureAwait(continueOnCapturedContext: false);
            return await BuildResponse(response);
        }

        private HttpRequestMessage BuildRequestMessage(IHttpRequest request)
        {
            var message = new HttpRequestMessage(HttpMethod.Post, request.Url);
            foreach (var headerName in request.Headers.GetAllHeaderNames().Where(IsStandardHeaderName))
            {
                message.Headers.Add(headerName, request.Headers.GetAllValues(headerName));
            }

            var acceptHeader = request.Headers.GetAllHeaderNames()
                .FirstOrDefault(x => x.Equals("accept", StringComparison.CurrentCultureIgnoreCase));
            if (acceptHeader != null)
            {
                message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(request.Headers.GetValue(acceptHeader)));
            }

            if (request.HasContent())
            {
                message.Content = GetContent(request);
            }

            return message;
        }

        private static async Task<HttpResponse> BuildResponse(HttpResponseMessage response)
        {
            if (response == null) throw new Exception("Unable to read web response");

            return new HttpResponse((int) response.StatusCode, GetResponseHeaders(response),
                await response.Content.ReadAsStringAsync());
        }

        private static HttpContent GetContent(IHttpRequest request)
        {
            var content = request.GetContent();
            var contentTypeHeader = request.Headers.GetAllHeaderNames()
                .FirstOrDefault(x => x.Equals("content-type", StringComparison.CurrentCultureIgnoreCase));
            if (contentTypeHeader != null)
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(request.Headers.GetValue(contentTypeHeader));
            }

            return content;
        }

        private static HttpHeaders GetResponseHeaders(HttpResponseMessage response)
        {
            var headers = new HttpHeaders();

            headers.AddAll(response.Headers);
            headers.AddAll(response.Content.Headers);
            return headers;
        }

        private static bool IsStandardHeaderName(string name)
        {
            if (name.Equals("accept", StringComparison.CurrentCultureIgnoreCase)) return false;
            if (name.Equals("content-type", StringComparison.CurrentCultureIgnoreCase)) return false;

            return true;
        }
    }
}
