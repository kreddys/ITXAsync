namespace ITXAsync
{
    using System;
    using System.Collections.Generic;

    using System.Globalization;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    public partial class HttpPostInput
    {
        [JsonProperty("ITXFencedMapCall")]
        public ItxFencedMapCall ItxFencedMapCall { get; set; }
    }

    public partial class ItxFencedMapCall
    {
        [JsonProperty("ITXURL")]
        public Uri Itxurl { get; set; }

        [JsonProperty("frameworkMapName")]
        public string FrameworkMapName { get; set; }

        [JsonProperty("testOptions")]
        public TestOptions TestOptions { get; set; }

        [JsonProperty("mapOptions")]
        public MapOptions MapOptions { get; set; }
    }

    public partial class MapOptions
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("audit")]
        public bool Audit { get; set; }

        [JsonProperty("trace")]
        public bool Trace { get; set; }

        [JsonProperty("inputs")]
        public Put[] Inputs { get; set; }

        [JsonProperty("outputs")]
        public Put[] Outputs { get; set; }
    }

    public partial class Put
    {
        [JsonProperty("cardNumber")]
        public long CardNumber { get; set; }

        [JsonProperty("source", NullValueHandling = NullValueHandling.Ignore)]
        public string Source { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("fileName")]
        public string FileName { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
    }

    public partial class TestOptions
    {
        [JsonProperty("waitSecs")]
        public long WaitSecs { get; set; }
    }
}
