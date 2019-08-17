using System;
using System.IO;
using Newtonsoft.Json;

namespace repo_version.Commands
{
    public abstract class ModifyConfigCommand : ICommand
    {
        private readonly bool _create;

        protected ModifyConfigCommand(bool create)
        {
            _create = create;
        }

        public int Execute(Options options)
        {
            var finder = new GitFolderFinder();
            var gitFolder = finder.FindGitFolder(options.Path);

            if (gitFolder == null)
            {
                Console.WriteLine("not a git repository");
                return 1;
            }

            var config = Configuration.Load(gitFolder, _create);

            if (config == null)
            {
                Console.WriteLine("No repo-version.json file. Please run repo-version init");
                return 1;
            }

            TransformConfiguration(options, config);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            var path = Path.Combine(gitFolder, "repo-version.json");
            File.WriteAllText(path, json);

            OnSuccess(config, path);
            return 0;
        }

        protected abstract void TransformConfiguration(Options options, Configuration config);

        protected abstract void OnSuccess(Configuration config, string path);
    }
}
