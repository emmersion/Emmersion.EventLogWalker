using System;
using System.Threading.Tasks;
using Emmersion.EventLogWalker.Configuration;
using Emmersion.EventLogWalker.Http;

namespace Emmersion.EventLogWalker
{
    internal class InsightsSystemApiPager : IPager<InsightEvent>
    {
        private readonly IHttpClient httpClient;
        private readonly IInsightsSystemApiSettings insightsSystemApiSettings;
        private readonly IJsonSerializer jsonSerializer;

        public InsightsSystemApiPager(IHttpClient httpClient,
            IInsightsSystemApiSettings insightsSystemApiSettings,
            IJsonSerializer jsonSerializer)
        {
            this.httpClient = httpClient;
            this.insightsSystemApiSettings = insightsSystemApiSettings;
            this.jsonSerializer = jsonSerializer;
        }

        public async Task<Page<InsightEvent>> GetPageAsync(Cursor cursor)
        {
            var request = new HttpRequest
            {
                Url = $"{insightsSystemApiSettings.BaseUrl}/event-log/page",
                Headers = new HttpHeaders().Add("Authorization", $"Bearer {insightsSystemApiSettings.ApiKey}")
                    .Add("Content-Type", "application/json"),
                Body = jsonSerializer.Serialize(cursor)
            };

            var response = await httpClient.ExecutePostAsync(request);

            var result = response.StatusCode switch
            {
                200 => jsonSerializer.Deserialize<Page<InsightEvent>>(response.Body),
                403 => throw new Exception(
                    $"Double check your credentials: Got status code {response.StatusCode} when calling {request.Url} with body {request.Body}"),
                _ => throw new Exception(
                    $"Unexpected status code {response.StatusCode} when calling {request.Url} with body {request.Body}")
            };

            return result;
        }
    }
}
