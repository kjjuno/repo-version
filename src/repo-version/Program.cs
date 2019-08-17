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

            var folderFinder = new GitFolderFinder();
            var tagFinder = new TaggedVersionFinder();
            var labelCalculator = new LabelCalculator();
            var versionCalculator = new VersionCalculator(folderFinder, tagFinder, labelCalculator);

            if (options.ShowAssemblyVersion)
            {
                command = new ShowAssemblyVersionCommand();
            }
            else if (options.Verb == "init")
            {
                command = new InitConfigCommand(versionCalculator);
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
                command = new TagCommand(versionCalculator, folderFinder);
            }
            else
            {
                command = new DisplayVersionCommand(versionCalculator);
            }

            code = command?.Execute(options) ?? -1;

            return code;
        }
    }
}
