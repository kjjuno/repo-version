using System.Collections.Generic;

namespace repo_version
{
    class Configuration
    {
        public int Major { get; set; } 
        public int Minor { get; set; } 
        public List<BranchConfig> Branches { get; set; }
    }
}
