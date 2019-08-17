using Newtonsoft.Json;

namespace repo_version
{
    public class BranchConfig
    {
        [JsonProperty("regex", Order = 0)]
        public string Regex { get; set; }

        [JsonProperty("defaultLabel", Order = 1)]
        public string DefaultLabel { get; set; }

        [JsonProperty("mainline", Order = 2)]
        public bool IsMainline { get; set; }
    }
}
