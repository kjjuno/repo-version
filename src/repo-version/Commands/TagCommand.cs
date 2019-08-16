using System;
using LibGit2Sharp;

namespace repo_version.Commands
{
    class TagCommand : ICommand
    {
        public int Execute(Options options)
        {
            var calculator = new VersionCalculator();
            var finder = new GitFolderFinder();

            var version = calculator.CalculateVersion(options);

            if (version == null)
            {
                return 1;
            }

            if (version.IsDirty)
            {
                Console.WriteLine("Cannot apply tag with uncommitted changes");
                return 1;
            }

            var gitFolder = finder.FindGitFolder(options.Path);

            using (var repo = new Repository(gitFolder))
            {
                repo.ApplyTag(version.SemVer);
            }

            Console.WriteLine($"Created Tag: {version.SemVer}");

            return 0;
        }
    }
}
