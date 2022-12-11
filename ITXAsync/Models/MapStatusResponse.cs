namespace ITXAsync
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class MapStatusResponse
    {
        [JsonProperty("status")]
        public long Status { get; set; }

        [JsonProperty("outputs")]
        public Output[] Outputs { get; set; }

        [JsonProperty("start_timestamp")]
        public string StartTimestamp { get; set; }

        [JsonProperty("elapsed_time")]
        public long ElapsedTime { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }

        [JsonProperty("audit_href")]
        public Uri AuditHref { get; set; }

        [JsonProperty("trace_href")]
        public Uri TraceHref { get; set; }

        public override string ToString()
        {
            var outputString = "";
            foreach (var output in Outputs)
            {
                outputString += $"output.href: {output.Href}, output.card_number: {output.CardNumber}, output.mime_type: {output.MimeType}";
            }

            return $"status: {Status}, status_message: {StatusMessage}, elapsed_time: {ElapsedTime}, outputs: [{outputString}]";
        }
    }

    public partial class Output
    {
        [JsonProperty("href")]
        public Uri Href { get; set; }

        [JsonProperty("card_number")]
        public long CardNumber { get; set; }

        [JsonProperty("mime_type")]
        public string MimeType { get; set; }
    }
}
