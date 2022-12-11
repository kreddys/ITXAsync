using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITXAsync
{
    public partial class CallBackRequest
    {
        [JsonProperty("callBackUri")]
        public Uri CallBackUri { get; set; }

        [JsonProperty("response")]
        public Response Response { get; set; }

    }

    public partial class Response
    {
        [JsonProperty("Output")]
        public MapStatusResponse Output { get; set; }

        [JsonProperty("Error")]
        public Error Error { get; set; }

        [JsonProperty("StatusCode")]
        public string StatusCode { get; set; }
    }

    public partial class Error
    {
        [JsonProperty("ErrorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("Message")]
        public string Message { get; set; }
    }
}
