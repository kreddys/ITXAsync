namespace ITXAsync
{
    using System;
    using Newtonsoft.Json;

    public partial class MapRequest
    {
        [JsonProperty("itxUri")]
        public Uri ItxUri { get; set; }

        [JsonProperty("frameworkMap")]
        public FrameworkMap FrameworkMap { get; set; }

        [JsonProperty("runMap")]
        public RunMap RunMap { get; set; }

        [JsonProperty("callBackUri")]
        public Uri CallBackUri { get; set; }
    }

    public partial class FrameworkMap
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("audit")]
        public bool Audit { get; set; }

        [JsonProperty("trace")]
        public bool Trace { get; set; }

        [JsonProperty("inputCard")]
        public int InputCard { get; set; }

        [JsonProperty("outputCard")]
        public int OutputCard { get; set; }
    }

    public partial class RunMap
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("audit")]
        public bool Audit { get; set; }

        [JsonProperty("trace")]
        public bool Trace { get; set; }

        [JsonProperty("waitSeconds")]
        public long WaitSeconds { get; set; }

        [JsonProperty("maxRetries")]
        public long MaxRetries { get; set; }

        [JsonProperty("inputs")]
        public Put[] Inputs { get; set; }

        [JsonProperty("outputs")]
        public Put[] Outputs { get; set; }
    }

    public partial class Put
    {
        [JsonProperty("cardNumber")]
        public long CardNumber { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }

        [JsonProperty("file")]
        public string File { get; set; }
    }
}
