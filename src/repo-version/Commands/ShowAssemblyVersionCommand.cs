using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace repo_version.Commands
{
    class ShowAssemblyVersionCommand : ICommand
    {
        public int Execute(Options options)
        {
            IEnumerable<Attribute> attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute));
            var attr = attrs?.FirstOrDefault() as AssemblyInformationalVersionAttribute;

            if (attr != null)
            {
                Console.WriteLine($"repo-version: {attr.InformationalVersion}");
                return 0;
            }

            Console.WriteLine("Something went wrong");
            return 1;
        }
    }
}
