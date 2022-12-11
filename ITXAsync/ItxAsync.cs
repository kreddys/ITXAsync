using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ITXAsync
{
    public static class ItxAsync
    {
        [FunctionName("ItxAsync")]
        public static async Task<dynamic> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {

            MapRequest mapRequest = context.GetInput<MapRequest>();

            var statusUrl = await context.CallActivityAsync<string>(nameof(InitiateRequest), mapRequest);

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(mapRequest.RunMap.WaitSeconds), (int)mapRequest.RunMap.MaxRetries);
            MapStatusResponse response = await context.CallActivityWithRetryAsync<MapStatusResponse>(nameof(FetchResult), retryOptions, statusUrl);

            CallBackRequest callBackReq = BuildCallback(new { response = response, callBackUri = mapRequest.CallBackUri });

            await context.CallActivityAsync<string>(nameof(SendCallback), callBackReq);

            return response;

        }

        [FunctionName("BuildCallback")]
        private static CallBackRequest BuildCallback(dynamic mapRequest)
        {
            Response callbackResponse = new Response();
            CallBackRequest cbr = new CallBackRequest();

            cbr.CallBackUri = mapRequest.callBackUri;

            if (mapRequest.response.StatusMessage == "Map completed successfully")
            {
                callbackResponse.Output = mapRequest.response;
                cbr.Response = callbackResponse;
            }
            else
            {
                callbackResponse.Output = mapRequest.response;
                cbr.Response = callbackResponse;
                cbr.Response.StatusCode = (mapRequest.response.Status + 400).ToString();

                Error err = new Error();

                err.Message = mapRequest.response.StatusMessage;
                err.ErrorCode = (mapRequest.response.Status).ToString();

                cbr.Response.Error = err;
            }

            return cbr;
        }

        [FunctionName(nameof(InitiateRequest))]
        public static async Task<string> InitiateRequest([ActivityTrigger] MapRequest mapRequest, ILogger log)
        {
            UriBuilder uriBuilder = new UriBuilder(mapRequest.ItxUri);
            uriBuilder.Path = mapRequest.ItxUri.AbsolutePath + mapRequest.FrameworkMap.Name;
            uriBuilder.Query = "input="+mapRequest.FrameworkMap.InputCard.ToString()+"&"+"output="+mapRequest.FrameworkMap.OutputCard.ToString();

            if (mapRequest.FrameworkMap.Audit == true && mapRequest.FrameworkMap.Trace == true)
            {
                uriBuilder.Query += "&return=audit,trace";
            }
            if (mapRequest.FrameworkMap.Audit == true && mapRequest.FrameworkMap.Trace == false)
            {
                uriBuilder.Query += "&return=audit";
            }
            if (mapRequest.FrameworkMap.Audit == false && mapRequest.FrameworkMap.Trace == true)
            {
                uriBuilder.Query += "&return=trace";
            }

            Uri url = uriBuilder.Uri;

            log.LogInformation($"Initiating request to {url}");

            using (var client = HttpClientFactory.Create())
            {
                var response = await client.PostAsJsonAsync(url, mapRequest.RunMap);
                response.EnsureSuccessStatusCode();
                var statusUrl = response.Headers.Location;
                log.LogInformation($"Status URL: {statusUrl}");
                return statusUrl.ToString();
            }
        }

        //FetchResult

        [FunctionName("FetchResult")]
        public static async Task<MapStatusResponse> FetchResult([ActivityTrigger] string statusUrl, ILogger log)
        {

            var client = new HttpClient();
            var response = await client.GetAsync(statusUrl);
            response.EnsureSuccessStatusCode();

            MapStatusResponse body = await response.Content.ReadAsAsync<MapStatusResponse>();

            log.LogInformation($"start_timestamp: {body.StartTimestamp}, status_message: {body.StatusMessage}, elapsed_time: {body.ElapsedTime}");

            if (body?.StatusMessage == "In progress")
            {
                throw new Exception($"start_timestamp: {body.StartTimestamp}, status_message: {body.StatusMessage}, elapsed_time: {body.ElapsedTime}");
            }
            else
            {
                return body;
            }
            
        }

        [FunctionName("SendCallback")]
        public static async Task SendCallback([ActivityTrigger] CallBackRequest callbackReq, ILogger log)
        {

            var client = new HttpClient();
            var response = await client.PostAsJsonAsync(callbackReq.CallBackUri, callbackReq.Response);
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadAsStringAsync();
        }
        
        [FunctionName("ItxAsync_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            MapRequest data = await req.Content.ReadAsAsync<MapRequest>();

            string instanceId = await starter.StartNewAsync("ItxAsync", null, data);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}