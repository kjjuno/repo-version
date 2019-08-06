using Newtonsoft.Json;

namespace repo_version
{
    class BranchConfig
    {
        [JsonProperty("regex", Order = 0)]
        public string Regex { get; set; }

        [JsonProperty("tag", Order = 0)]
        public string Tag { get; set; }
    }
}
