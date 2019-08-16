using System;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace repo_version
{
    class VersionCalculator
    {
        public RepoVersion CalculateVersion(Options options)
        {
            var finder = new GitFolderFinder();
            var gitFolder = finder.FindGitFolder(options.Path);

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

            var label = "";
            var status = repo.RetrieveStatus();

            // If nothing bumped it from the initial version
            // set the version to 0.1.0
            if (major == 0 & minor == 0 && patch == 0)
            {
                minor = 1;
            }

            if (commitsSinceTag > 0)
            {
                commits += commitsSinceTag;

                // Only increase the patch if there is no pre-release tag on the last git tag
                if (lastTag?.Label == "")
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

            var (configLabel, isMainline) = CalculateLabel(repo, config);

            // If we are exactly on a git tag use that pre-release value.
            if (lastTag != null && commitsSinceTag == 0 && !status.IsDirty)
            {
                label = lastTag.Label;
            }
            // If a pre-release tag was specified on the command line use it.
            else if (options.Label != null)
            {
                label = options.Label;
            }
            // If on a mainline branch but on a tagged commit, but there is a
            // previous tag for the same {major}.{minor}.{patch}, use the
            // pre-release tag from the last tag
            else if (isMainline && lastTag != null && lastTag.Major == major && lastTag.Minor == minor && lastTag.Patch == patch)
            {
                label = lastTag.Label;
            }
            else
            {
                label = configLabel;
            }

            var version = new RepoVersion
            {
                Major = major,
                Minor = minor,
                Patch = patch,
                Commits = commits,
                Label = label,
                IsDirty = status.IsDirty
            };

            return version;
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

        private static (string, bool) CalculateLabel(Repository repo, Configuration config)
        {
            bool found = false;
            var label = "";
            var isMainline = false;
            var branch = repo.Head.FriendlyName;

            if (config?.Branches != null)
            {
                foreach (var branchConfig in config.Branches)
                {
                    if (Regex.IsMatch(branch, branchConfig.Regex))
                    {
                        label = branchConfig.DefaultLabel ?? "";
                        isMainline = branchConfig.IsMainline;
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                label = "{BranchName}";
            }

            if (label.Contains("{BranchName}"))
            {
                var idx = branch.LastIndexOf('/');
                if (idx >= 0)
                {
                    branch = branch.Substring(idx + 1);
                }

                label = label
                    .Replace("{BranchName}", branch);
            }

            label = label
                .Replace("(no branch)", "detached-head")
                .Replace('_', '-')
                .Replace("(", "")
                .Replace(")", "")
                .Replace(" ", "-");

            label = label
                .Substring(0, Math.Min(30, label.Length))
                .TrimEnd('-');

            return (label, isMainline);
        }
    }
}
