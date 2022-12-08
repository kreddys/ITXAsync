using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace ITXAsync
{
    public static class ItxAsyncDurable
    {
        [FunctionName("ItxAsyncDurable")]
        public static async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {

            log.LogInformation("Starting orchestration");

            var input = context.GetInput<HttpPostInput>();

            var statusUrl = await context.CallActivityAsync<string>(nameof(InitiateRequest), input);

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(3), 10);
            var resultUrl = await context.CallActivityWithRetryAsync<string>(nameof(FetchResult), retryOptions, statusUrl);

            await context.CallActivityAsync(nameof(StoreResult), resultUrl);

        }

        //FetchResult

        [FunctionName("FetchResult")]
        public static async Task<string> FetchResult([ActivityTrigger] string statusUrl, ILogger log)
        {
            log.LogInformation("Fetching result");

            var client = new HttpClient();
            var response = await client.GetAsync(statusUrl);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsAsync<dynamic>();

            log.LogInformation($"status: {body.status}");
            log.LogInformation($"status_message: {body.status_message}");

            if (body?.status_message == "Map completed successfully")
            {
                return body.outputs[0].href;
            }
            else
            {
                throw new ApplicationException("not finished");
            }
        }

        //Store Result

        [FunctionName("StoreResult")]
        public static async Task StoreResult([ActivityTrigger] string resultUrl, ILogger log)
        {
            log.LogInformation("Storing result");

            var client = new HttpClient();
            var response = await client.GetAsync(resultUrl);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsStringAsync();
            File.WriteAllText("C:\\itx-rt\\examples\\data\\outputdata.txt", data);

            log.LogInformation("Data saved to local file.");

        }

        [FunctionName(nameof(InitiateRequest))]
        public static async Task<string> InitiateRequest([ActivityTrigger] HttpPostInput httpPostInput,ILogger log)
        {
            using (var client = HttpClientFactory.Create())
            {
                var response = await client.PostAsJsonAsync(httpPostInput.ItxFencedMapCall.Itxurl, httpPostInput);
                response.EnsureSuccessStatusCode();
                var statusUrl = response.Headers.Location;
                log.LogInformation($"Status URL: {statusUrl}");
                return statusUrl.ToString();
            }
        }
        [FunctionName("ItxAsyncDurable_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            dynamic data = await req.Content.ReadAsAsync<object>();
            
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("ItxAsyncDurable", null, data);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}