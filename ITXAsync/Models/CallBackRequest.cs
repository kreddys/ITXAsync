using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ITXAsync
{
    public partial class CallBackRequestWithUri
    {
        [JsonProperty("callBackUri")]
        public Uri CallBackUri { get; set; }

        [JsonProperty("callBackRequest")]
        public CallBackRequest CallBackRequest { get; set; }

    }

    public partial class CallBackRequest
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
