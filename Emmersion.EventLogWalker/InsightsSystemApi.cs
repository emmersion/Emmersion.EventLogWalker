using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Emmersion.EventLogWalker.Configuration;
using Emmersion.Http;

namespace Emmersion.EventLogWalker
{
    public interface IInsightsSystemApi
    {
        Task<Page> GetPageAsync(Cursor cursor);
    }

    public class InsightsSystemApi : IInsightsSystemApi
    {
        private readonly IHttpClient httpClient;
        private readonly IInsightsSystemApiSettings insightsSystemApiSettings;
        private readonly IJsonSerializer jsonSerializer;

        public InsightsSystemApi(IHttpClient httpClient,
            IInsightsSystemApiSettings insightsSystemApiSettings,
            IJsonSerializer jsonSerializer)
        {
            this.httpClient = httpClient;
            this.insightsSystemApiSettings = insightsSystemApiSettings;
            this.jsonSerializer = jsonSerializer;
        }

        public async Task<Page> GetPageAsync(Cursor cursor)
        {
            var request = new HttpRequest
            {
                Method = HttpMethod.POST,
                Url = $"{insightsSystemApiSettings.BaseUrl}/event-log/page",
                Headers = new HttpHeaders().Add("Authorization", $"Bearer {insightsSystemApiSettings.ApiKey}")
                    .Add("Content-Type", "application/json"),
                Body = jsonSerializer.Serialize(cursor)
            };

            var response = await httpClient.ExecuteAsync(request);

            return response.StatusCode switch
            {
                200 => jsonSerializer.Deserialize<Page>(response.Body),
                403 => throw new Exception(
                    $"Double check your credentials: Got status code {response.StatusCode} when calling {request.Url} with body {request.Body}"),
                _ => throw new Exception(
                    $"Unexpected status code {response.StatusCode} when calling {request.Url} with body {request.Body}")
            };
        }
    }

    public class Page
    {
        public List<InsightEvent> Events { get; set; }
        public Cursor NextPage { get; set; }
    }

    public class Cursor
    {
        public DateTimeOffset StartInclusive { get; set; }
        public DateTimeOffset EndExclusive { get; set; }
    }
}
