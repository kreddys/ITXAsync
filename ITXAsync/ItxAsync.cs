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

            MapStatusResponse response = new MapStatusResponse();

            try
            {
                response = await context.CallActivityWithRetryAsync<MapStatusResponse>(nameof(FetchResult), retryOptions, statusUrl);
            }
            catch (InProgressException ex)
            {
                // Ignore
            }
            catch (FunctionFailedException ex)
            {
                response.Outputs = null;
                response.Status = 408;
                response.StatusMessage = "Map Not Completed Within Timeout";
            }



            CallBackRequestWithUri callBackReqWithUri = BuildCallback(new { response = response, callBackUri = mapRequest.CallBackUri });

            await context.CallActivityAsync<string>(nameof(SendCallback), callBackReqWithUri);

            return callBackReqWithUri.CallBackRequest;

        }

        [FunctionName("BuildCallback")]
        private static CallBackRequestWithUri BuildCallback(dynamic data)
        {
            CallBackRequest cbr = new CallBackRequest();
            CallBackRequestWithUri cbrwuri = new CallBackRequestWithUri();

            cbrwuri.CallBackUri = data.callBackUri;

            if (data.response.StatusMessage == "Map completed successfully")
            {
                cbr.Output = data.response;
                cbr.StatusCode = data.response.Status.ToString();
            }
            else
            {
                cbr.Output = data.response;
                cbr.StatusCode = (data.response.Status + 400).ToString();

                Error err = new Error();

                err.Message = data.response.StatusMessage;
                err.ErrorCode = (data.response.Status).ToString();

                cbr.Error = err;
            }

            cbrwuri.CallBackRequest = cbr; 

            return cbrwuri;
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
                log.LogInformation($"start_timestamp: {body.StartTimestamp}, status_message: {body.StatusMessage}, elapsed_time: {body.ElapsedTime}");
                throw new InProgressException(body.ElapsedTime.ToString());
            }
            else
            {
                return body;
            }
            
        }

        [FunctionName("SendCallback")]
        public static async Task SendCallback([ActivityTrigger] CallBackRequestWithUri callbackReqWithUri, ILogger log)
        {

            var client = new HttpClient();
            var response = await client.PostAsJsonAsync(callbackReqWithUri.CallBackUri, callbackReqWithUri.CallBackRequest);
            response.EnsureSuccessStatusCode();
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