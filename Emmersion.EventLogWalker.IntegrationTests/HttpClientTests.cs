using System.Threading.Tasks;
using Emmersion.EventLogWalker.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.IntegrationTests
{
    [TestFixture]
    public class HttpClientTests
    {
        [SetUp]
        public void SetUp()
        {
            client = new HttpClient();
        }

        [TearDown]
        public void TearDown()
        {
            client.Dispose();
        }

        private HttpClient client;

        private static HttpBinPostResponse GetHttpBinResponse(HttpResponse response)
        {
            return JsonConvert.DeserializeObject<HttpBinPostResponse>(response.Body);
        }

        [Test]
        public async Task ResponseIncludesContentHeaders()
        {
            var request = new HttpRequest {Url = "http://httpbin.org/post", Body = ""};
            request.Headers.Add("Accept", "application/xml");
            request.Headers.Add("Content-Type", "application/json");

            var response = await client.ExecutePostAsync(request);

            Assert.That(response.Headers.Exists("Content-Type"));
        }
        
        [Test]
        public async Task WhenPerformingSimplePostingWithJson()
        {
            var request = new HttpRequest {Url = "http://httpbin.org/post", Body = "{\"username\":\"standard-user\", \"password\":\"testing1\"}"};

            var response = await client.ExecutePostAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(expected: 200));
            var responseData = GetHttpBinResponse(response);
            Assert.That(responseData.Data, Is.EqualTo(request.Body));
        }

        [Test]
        public async Task WhenSendingHeaders()
        {
            var request = new HttpRequest {Url = "http://httpbin.org/post", Body = ""};
            request.Headers.Add("Accept", "application/xml");
            request.Headers.Add("Content-Type", "application/json");
            request.Headers.Add("Authorization", "token abc123");

            var response = await client.ExecutePostAsync(request);

            var headers = (JObject) JObject.Parse(response.Body).GetValue("headers");
            Assert.That(headers["Accept"].Value<string>(), Is.EqualTo("application/xml"));
            Assert.That(headers["Content-Type"].Value<string>(), Is.EqualTo("application/json"));
            Assert.That(headers["Authorization"].Value<string>(), Is.EqualTo("token abc123"));
        }
    }

    public class HttpBinPostResponse
    {
        public string Data { get; set; }
    }
}
