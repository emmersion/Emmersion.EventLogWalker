using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Emmersion.EventLogWalker.Configuration;
using Emmersion.EventLogWalker.Http;
using Emmersion.Testing;
using Moq;
using NUnit.Framework;

namespace Emmersion.EventLogWalker.UnitTests
{
    internal class InsightsSystemApiTests : With_an_automocked<InsightsSystemApiPager>
    {
        [Test]
        public async Task When_getting_a_page()
        {
            var cursor = new Cursor();
            var cursorJson = RandomString();
            var insightsSystemApiBaseUrl = RandomString();
            var insightsSystemApiApiKey = RandomString();
            var serializedApiPage = RandomString();
            var deserializedApiPage = new Page<InsightEvent>
            {
                Events = new List<InsightEvent>
                {
                    new InsightEvent()
                },
                NextPage = new Cursor(),
            };

            HttpRequest capturedHttpRequest = null;

            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.BaseUrl).Returns(insightsSystemApiBaseUrl);
            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.ApiKey).Returns(insightsSystemApiApiKey);
            GetMock<IHttpClient>().Setup(x => x.ExecutePostAsync(IsAny<IHttpRequest>()))
                .Callback<IHttpRequest>(httpRequest => capturedHttpRequest = httpRequest as HttpRequest)
                .ReturnsAsync(new HttpResponse(200, new HttpHeaders(), serializedApiPage));
            GetMock<IJsonSerializer>().Setup(x => x.Deserialize<Page<InsightEvent>>(serializedApiPage)).Returns(deserializedApiPage);
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(cursor)).Returns(cursorJson);

            var page = await ClassUnderTest.GetPageAsync(cursor);

            Assert.That(capturedHttpRequest, Is.Not.Null);
            Assert.That(capturedHttpRequest.Url, Is.EqualTo($"{insightsSystemApiBaseUrl}/event-log/page"));
            Assert.That(capturedHttpRequest.Headers.Exists("Authorization"), Is.True);
            Assert.That(capturedHttpRequest.Headers.GetValue("Authorization"), Is.EqualTo($"Bearer {insightsSystemApiApiKey}"));
            Assert.That(capturedHttpRequest.Headers.Exists("Content-Type"), Is.True);
            Assert.That(capturedHttpRequest.Headers.GetValue("Content-Type"), Is.EqualTo("application/json"));
            Assert.That(capturedHttpRequest.Body, Is.EqualTo(cursorJson));
            Assert.That(page.Events.Count, Is.EqualTo(deserializedApiPage.Events.Count));
            Assert.That(page.Events.Single(), Is.SameAs(deserializedApiPage.Events.Single()));
            Assert.That(page.NextPage, Is.SameAs(deserializedApiPage.NextPage));
        }

        [Test]
        public void When_getting_a_page_is_not_successful()
        {
            var cursor = new Cursor();
            var cursorJson = RandomString();
            var insightsSystemApiBaseUrl = RandomString();
            var non200StatusCode = 400;

            GetMock<IInsightsSystemApiSettings>().SetupGet(x => x.BaseUrl).Returns(insightsSystemApiBaseUrl);
            GetMock<IHttpClient>().Setup(x => x.ExecutePostAsync(IsAny<IHttpRequest>()))
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
            GetMock<IHttpClient>().Setup(x => x.ExecutePostAsync(IsAny<IHttpRequest>()))
                .ReturnsAsync(new HttpResponse(notAuthorizedStatusCode, new HttpHeaders(), ""));
            GetMock<IJsonSerializer>().Setup(x => x.Serialize(cursor)).Returns(cursorJson);

            var exception = Assert.ThrowsAsync<Exception>(() => ClassUnderTest.GetPageAsync(cursor));

            var url = $"{insightsSystemApiBaseUrl}/event-log/page";
            Assert.That(exception?.Message, Is.EqualTo($"Double check your credentials: Got status code {notAuthorizedStatusCode} when calling {url} with body {cursorJson}"));
        }
    }
}
