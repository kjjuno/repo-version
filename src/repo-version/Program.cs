using System;
using repo_version.Commands;

namespace repo_version
{
    class Program
    {

        static int Main(string[] args)
        {
            var options = Options.Parse(args);

            if (options.ShowHelp)
            {
                Console.WriteLine(options.PrintHelp());
                return 1;
            }

            var code = 0;
            ICommand command = null;

            if (options.ShowAssemblyVersion)
            {
                command = new ShowAssemblyVersionCommand();
            }
            else if (options.Verb == "init")
            {
                command = new InitConfigCommand();
            }
            else if (options.Verb == "major")
            {
                command = new BumpMajorVersionCommand();
            }
            else if (options.Verb == "minor")
            {
                command = new BumpMinorVersionCommand();
            }
            else if (options.Verb == "tag")
            {
                command = new TagCommand();
            }
            else
            {
                command = new DisplayVersionCommand();
            }

            code = command?.Execute(options) ?? -1;

            return code;
        }
    }
}
