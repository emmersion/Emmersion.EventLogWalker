using System;
using System.Threading.Tasks;
using Emmersion.EventLogWalker.Configuration;
using Emmersion.Http;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    public class InsightsSystemApiTests : With_an_automocked<InsightsSystemApi>
    {
        [Test]
        public async Task When_getting_a_page()
        {
            var cursor = new Cursor();
            var cursorJson = RandomString();
            var insightsSystemApiBaseUrl = RandomString();
            var insightsSystemApiApiKey = RandomString();
            var serializedApiPage = RandomString();
            var expectedPage = new Page();

            HttpRequest capturedHttpRequest = null;

            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.BaseUrl).Returns(insightsSystemApiBaseUrl);
            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.ApiKey).Returns(insightsSystemApiApiKey);
            GetMock<IHttpClient>().Setup(x => x.ExecuteAsync(IsAny<IHttpRequest>()))
                .Callback<IHttpRequest>(httpRequest => capturedHttpRequest = httpRequest as HttpRequest)
                .ReturnsAsync(new HttpResponse(200, new HttpHeaders(), serializedApiPage));
            GetMock<IJsonSerializer>().Setup(x => x.Deserialize<Page>(serializedApiPage)).Returns(expectedPage);
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(cursor)).Returns(cursorJson);

            var page = await ClassUnderTest.GetPageAsync(cursor);

            Assert.That(capturedHttpRequest, Is.Not.Null);
            Assert.That(capturedHttpRequest.Method, Is.EqualTo(HttpMethod.POST));
            Assert.That(capturedHttpRequest.Url, Is.EqualTo($"{insightsSystemApiBaseUrl}/event-log/page"));
            Assert.That(capturedHttpRequest.Headers.Exists("Authorization"), Is.True);
            Assert.That(capturedHttpRequest.Headers.GetValue("Authorization"), Is.EqualTo($"Bearer {insightsSystemApiApiKey}"));
            Assert.That(capturedHttpRequest.Headers.Exists("Content-Type"), Is.True);
            Assert.That(capturedHttpRequest.Headers.GetValue("Content-Type"), Is.EqualTo("application/json"));
            Assert.That(capturedHttpRequest.Body, Is.EqualTo(cursorJson));
            Assert.That(page, Is.SameAs(expectedPage));
        }

        [Test]
        public void When_getting_a_page_is_not_successful()
        {
            var cursor = new Cursor();
            var cursorJson = RandomString();
            var insightsSystemApiBaseUrl = RandomString();
            var non200StatusCode = 400;

            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.BaseUrl).Returns(insightsSystemApiBaseUrl);
            GetMock<IHttpClient>().Setup(x => x.ExecuteAsync(IsAny<IHttpRequest>()))
                .ReturnsAsync(new HttpResponse(non200StatusCode, new HttpHeaders(), ""));
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(cursor)).Returns(cursorJson);

            var exception = Assert.ThrowsAsync<Exception>(() => ClassUnderTest.GetPageAsync(cursor));

            var url = $"{insightsSystemApiBaseUrl}/event-log/page";
            Assert.That(exception?.Message, Is.EqualTo($"Unexpected status code {non200StatusCode} when calling {url} with body {cursorJson}"));
        }
        
        [Test]
        public void When_getting_a_page_and_not_authorized()
        {
            var cursor = new Cursor();
            var cursorJson = RandomString();
            var insightsSystemApiBaseUrl = RandomString();
            var notAuthorizedStatusCode = 403;

            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.BaseUrl).Returns(insightsSystemApiBaseUrl);
            GetMock<IHttpClient>().Setup(x => x.ExecuteAsync(IsAny<IHttpRequest>()))
                .ReturnsAsync(new HttpResponse(notAuthorizedStatusCode, new HttpHeaders(), ""));
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(cursor)).Returns(cursorJson);

            var exception = Assert.ThrowsAsync<Exception>(() => ClassUnderTest.GetPageAsync(cursor));

            var url = $"{insightsSystemApiBaseUrl}/event-log/page";
            Assert.That(exception?.Message, Is.EqualTo($"Double check your credentials: Got status code {notAuthorizedStatusCode} when calling {url} with body {cursorJson}"));
        }
    }
}
