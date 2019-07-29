using System;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Newtonsoft.Json;
using Version = System.Version;

namespace repo_version
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = ".";
            if (args.Length > 0)
            {
                path = args[0];
            }

            var response = VersionCalculator.CalculateVersion(null);

            Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
        }


    }
}
