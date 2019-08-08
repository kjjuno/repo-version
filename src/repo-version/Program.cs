using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using LibGit2Sharp;
using Newtonsoft.Json;
using System.Linq;
using System.Reflection;

namespace repo_version
{
    class Program
    {

        static int Main(string[] args)
        {
            var op = Options.Parse(args);

            if (op.ShowHelp)
            {
                Console.WriteLine(op.PrintHelp());
                return 1;
            }

            var code = 0;

            if (op.ShowVersion)
            {
                code = ShowVersion();
            }
            else if (op.Verb == "init")
            {
                code = Init(op);
            }
            else if (op.Verb == "major")
            {
                code = BumpMajorVersion(op);
            }
            else if (op.Verb == "minor")
            {
                code = BumpMinorVersion(op);
            }
            else if (op.Verb == "tag")
            {
                code = ApplyTag(op);
            }
            else
            {
                code = DisplayVersion(op);
            }

            return code;
        }

        private static int ShowVersion()
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

        private static int ModifyConfig(string path, bool create, Action<Configuration> transform, Action<Configuration, string> success)
        {
            var gitFolder = FindGitFolder(path);

            if (gitFolder == null)
            {
                Console.WriteLine("not a git repository");
                return 1;
            }

            var config = Configuration.Load(gitFolder, create);

            if (config == null)
            {
                Console.WriteLine("No repo-version.json file. Please run repo-version init");
                return 1;
            }

            transform(config);

            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            path = Path.Combine(gitFolder, "repo-version.json");
            File.WriteAllText(path, json);

            success(config, path);
            return 0;
        }

        private static int Init(Options options)
        {
            return ModifyConfig(options.Path,
                create: true,
                transform: config =>
                {
                    var version = CalculateVersion(options);

                    config.Major = version.Major;
                    config.Minor = version.Minor;

                    Console.WriteLine("Please enter the major and minor versions for this repository");
                    Console.Write("Major: ({0}) ", version.Major);
                    var input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var major))
                    {
                        config.Major = major;
                    }

                    Console.Write("Minor: ({0}) ", version.Minor);
                    input = Console.ReadLine();

                    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out var minor))
                    {
                        config.Minor = minor;
                    }
                },
                success: (config, path) =>
                {
                    Console.WriteLine("created {0}", path);
                });
        }

        private static int BumpMajorVersion(Options options)
        {
            return ModifyConfig(options.Path,
                create: false,
                transform: config =>
                {
                    config.Major++;
                    config.Minor = 0;
                },
                success: (config, path) =>
                {
                    Console.WriteLine("Version bumped to {0}.{1}", config.Major, config.Minor);
                });
        }

        private static int BumpMinorVersion(Options options)
        {
            return ModifyConfig(options.Path,
                create: false,
                transform: config =>
                {
                    config.Minor++;
                },
                success: (config, path) =>
                {
                    Console.WriteLine("Version bumpted to {0}.{1}", config.Major, config.Minor);
                });
        }

        private static int ApplyTag(Options options)
        {
            var response = CalculateVersion(options);

            if (response == null)
            {
                return 1;
            }

            if (response.IsDirty)
            {
                Console.WriteLine("Cannot apply tag with uncommitted changes");
                return 1;
            }

            var gitFolder = FindGitFolder(options.Path);

            using (var repo = new Repository(gitFolder))
            {
                repo.ApplyTag(response.SemVer);
            }

            Console.WriteLine($"Created Tag: {response.SemVer}");

            return 0;
        }

        private static int DisplayVersion(Options options)
        {
            var response = CalculateVersion(options);

            if (response == null)
            {
                return 1;
            }

            if (string.Compare(options.Format, "json", StringComparison.OrdinalIgnoreCase) == 0)
            {
                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));
            }
            else
            {
                Console.WriteLine(response.SemVer);
            }

            return 1;
        }

        private static string FindGitFolder(string path)
        {
            var curr = new DirectoryInfo(path);

            while (curr != null && !Directory.Exists(Path.Combine(curr.FullName, ".git")))
            {
                curr = curr.Parent;
            }

            return curr?.FullName;
        }

        private static RepoVersion GetLastTaggedVersion(Repository repo, out int commitsSinceTag)
        {
            var queryTags = from t in repo.Tags
                let commit = t.PeeledTarget
                where commit != null
                select new
                {
                    Commit = commit,
                    Tag = t
                };

            var lookup = queryTags.ToLookup(x => x.Commit.Id, x => x.Tag);

            commitsSinceTag = 0;

            RepoVersion lastTag = null;

            foreach (var commit in repo.Commits)
            {
                var t = lookup[commit.Id];
                var tag = t.FirstOrDefault()?.FriendlyName.TrimStart('v', 'V');

                if (!string.IsNullOrEmpty(tag) && RepoVersion.TryParse(tag, out var v))
                {
                    lastTag = v;
                    break;
                }
                commitsSinceTag++;
            }

            return lastTag;
        }

        public static RepoVersion CalculateVersion(Options options)
        {
            var gitFolder = FindGitFolder(options.Path);

            if (gitFolder == null)
            {
                Console.WriteLine("not a git repository");
                return null;
            }

            var config = Configuration.Load(gitFolder, true);

            if (config == null)
            {
                return null;
            }

            var repo = new Repository(gitFolder);
            var lastTag = GetLastTaggedVersion(repo, out var commitsSinceTag);

            var major = lastTag?.Major ?? config.Major;
            var minor = lastTag?.Minor ?? config.Minor;
            var patch = lastTag?.Patch ?? 0;
            var commits = lastTag?.Commits ?? 0;

            var preReleaseTag = "";
            var status = repo.RetrieveStatus();

            // Use the pre-release tag specified by the tag on the current commit
            if (lastTag != null && commitsSinceTag == 0 && !status.IsDirty)
            {
                preReleaseTag = lastTag.PreReleaseTag;
            }
            // if no tag exists at the current commit, calculate the pre-release tag
            else
            {
                preReleaseTag = CalculatePreReleaseTag(repo, config);
            }

            // If nothing bumped it from the initial version
            // set the version to 0.1.0
            if (major == 0 & minor == 0 && patch == 0)
            {
                minor = 1;
            }
            else if (commitsSinceTag > 0)
            {
                commits += commitsSinceTag;

                // Only increase the patch if there is no pre-release tag on the last git tag
                if (lastTag?.PreReleaseTag == "")
                {
                    patch++;
                    commits = commitsSinceTag;
                }
            }

            // Use the major/minor from the config only if it is greater
            // than the calculated version
            if (config.Major > major)
            {
                major = config.Major;
                minor = config.Minor;
                patch = 0;
            }
            else if (config.Major == major && config.Minor > minor)
            {
                minor = config.Minor;
                patch = 0;
            }

            var response = new RepoVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                Commits = commits,
                PreReleaseTag = preReleaseTag,
                IsDirty = status.IsDirty
            };

            return response;
        }

        static RepoVersion ParseVersion(string input)
        {
            var str = input.TrimStart('v', 'V');

            if (!RepoVersion.TryParse(str, out var v))
            {
                return null;
            }

            return v;
        }

        private static string CalculatePreReleaseTag(Repository repo, Configuration config)
        {
            bool found = false;
            var preReleaseTag = "";
            var branch = repo.Head.FriendlyName;

            if (config?.Branches != null)
            {
                foreach (var branchConfig in config.Branches)
                {
                    if (Regex.IsMatch(branch, branchConfig.Regex))
                    {
                        preReleaseTag = branchConfig.Tag;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                preReleaseTag = "{BranchName}";
            }

            if (preReleaseTag.Contains("{BranchName}"))
            {
                var idx = branch.LastIndexOf('/');
                if (idx >= 0)
                {
                    branch = branch.Substring(idx + 1);
                }

                preReleaseTag = preReleaseTag
                    .Replace("{BranchName}", branch);
            }

            preReleaseTag = preReleaseTag
                .Replace("(no branch)", "detached-head")
                .Replace('_', '-')
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "-");

            preReleaseTag = preReleaseTag
                .Substring(0, Math.Min(30, preReleaseTag.Length))
                .TrimEnd('-');

            return preReleaseTag;
        }
    }
}
