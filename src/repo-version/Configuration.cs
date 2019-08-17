using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace repo_version
{
    public class Configuration
    {
        [JsonProperty("major", Order = 0)]
        public int Major { get; set; } 

        [JsonProperty("minor", Order = 1)]
        public int Minor { get; set; } 

        [JsonProperty("branches", Order = 2)]
        public List<BranchConfig> Branches { get; set; }

        public static Configuration Load(string path, bool useDefaults)
        {
            Configuration config = null;
            var defaultConfig = new Configuration();
            defaultConfig.Major = 0;
            defaultConfig.Minor = 1;
            defaultConfig.Branches = new List<BranchConfig>
            {
                new BranchConfig
                {
                    Regex = "^master$",
                    DefaultLabel = "",
                    IsMainline = true
                },
                new BranchConfig
                {
                    Regex = "^support[/-].*$",
                    DefaultLabel = "",
                    IsMainline = true
                },
                new BranchConfig
                {
                    Regex = ".+",
                    DefaultLabel = "{BranchName}",
                    IsMainline = false
                },
            };
            var configFile = Path.Combine(path, "repo-version.json");
            if (File.Exists(configFile))
            {
                try
                {
                    var json = File.ReadAllText(configFile);
                    config = JsonConvert.DeserializeObject<Configuration>(json);

                    if (config.Branches == null)
                    {
                        config.Branches = defaultConfig.Branches;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to load repo-version.json. {0}", e.Message);
                    return null;
                }
            }
            else if (useDefaults)
            {
                config = defaultConfig;
            }

            return config;
        }
    }
}
