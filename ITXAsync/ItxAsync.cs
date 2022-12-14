using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ITXAsync
{
    public class ItxAsync
    {
        private readonly HttpClient _httpClient;

        public ItxAsync(IHttpClientFactory httpClientFactory)
        {
            this._httpClient = httpClientFactory.CreateClient();
        }

        [FunctionName("ItxAsync")]
        public static async Task<dynamic> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {

            MapRequest mapRequest = context.GetInput<MapRequest>();

            var statusUrl = await context.CallActivityAsync<string>(nameof(InitiateRequest), mapRequest);

            var retryOptions = new RetryOptions(TimeSpan.FromSeconds(mapRequest.Map.WaitSeconds), (int)mapRequest.Map.MaxRetries);

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

            CallBackRequestWithUri callBackReqWithUri = BuildCallback(response, mapRequest.CallBackUri);

            await context.CallActivityAsync<string>(nameof(SendCallback), callBackReqWithUri);

            return callBackReqWithUri.CallBackRequest;

        }

        [FunctionName("BuildCallback")]
        private static CallBackRequestWithUri BuildCallback(MapStatusResponse response, Uri callBackUri)
        {
            CallBackRequest cbr = new CallBackRequest();
            CallBackRequestWithUri cbrwuri = new CallBackRequestWithUri();

            cbrwuri.CallBackUri = callBackUri;

            if (response.StatusMessage == "Map completed successfully")
            {
                cbr.Output = response;
                cbr.StatusCode = response.Status.ToString();
            }
            else
            {
                cbr.Output = response;
                cbr.StatusCode = (response.Status + 500).ToString();

                Error err = new Error();

                err.Message = response.StatusMessage;
                err.ErrorCode = (response.Status).ToString();

                cbr.Error = err;
            }

            cbrwuri.CallBackRequest = cbr; 

            return cbrwuri;
        }

        [FunctionName(nameof(InitiateRequest))]
        public async Task<string> InitiateRequest([ActivityTrigger] MapRequest mapRequest, ILogger log)
        {
            UriBuilder uriBuilder = new UriBuilder(mapRequest.ItxUri);
            uriBuilder.Path = mapRequest.ItxUri.AbsolutePath + mapRequest.Map.Name;

            List<KeyValuePair<string, string>> queryParams = new List<KeyValuePair<string, string>>();

            foreach (var input in mapRequest.Map.Inputs)
            {
                queryParams.Add(new KeyValuePair<string, string>("input", $"{input.CardNumber};{input.Source};{input.File}"));                
            }

            foreach (var output in mapRequest.Map.Outputs)
            {
                queryParams.Add(new KeyValuePair<string, string>("output", $"{output.CardNumber};{output.Source};{output.File}"));
            }

            foreach (var queryParam in queryParams)
            {
                string key = queryParam.Key;
                string value = queryParam.Value;

                // Append the query parameter to the Uri
                uriBuilder.Query = $"{uriBuilder.Query}&{key}={value}";
            }

            if (mapRequest.Map.Audit == true && mapRequest.Map.Trace == true)
            {
                uriBuilder.Query += "&return=audit,trace";
            }
            if (mapRequest.Map.Audit == true && mapRequest.Map.Trace == false)
            {
                uriBuilder.Query += "&return=audit";
            }
            if (mapRequest.Map.Audit == false && mapRequest.Map.Trace == true)
            {
                uriBuilder.Query += "&return=trace";
            }

            Uri url = uriBuilder.Uri;

            log.LogInformation($"Initiating request to {url}");

            Uri requestUri = new Uri(url.ToString());
            StringContent requestContent = new StringContent("", Encoding.UTF8);
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await _httpClient.PostAsync(requestUri, requestContent);
            response.EnsureSuccessStatusCode();
            var statusUrl = response.Headers.Location;
            log.LogInformation($"Status URL: {statusUrl}");

            return statusUrl.ToString();
        }

        //FetchResult

        [FunctionName("FetchResult")]
        public async Task<MapStatusResponse> FetchResult([ActivityTrigger] string statusUrl, ILogger log)
        {
            var response = await _httpClient.GetAsync(statusUrl);
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
        public async Task SendCallback([ActivityTrigger] CallBackRequestWithUri callbackReqWithUri, ILogger log)
        {

            var response = await _httpClient.PostAsJsonAsync(callbackReqWithUri.CallBackUri, callbackReqWithUri.CallBackRequest);
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