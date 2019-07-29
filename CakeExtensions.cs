using System;
using System.IO;
using Cake.Core;
using Cake.Core.Annotations;
using LibGit2Sharp;
using Version = System.Version;
using System.Linq;

namespace repo_version
{
    public static class VersionCalculator
    {
        static Version ParseVersion(string input)
        {
            var str = input.TrimStart('v', 'V');

            if (!Version.TryParse(str, out var v))
            {
                return null;
            }

            return v;
        }

        [CakeMethodAlias]
        public static object CalculateVersion(this ICakeContext context)
        {
            return CalculateVersion(context, ".");
        }

        [CakeMethodAlias]
        public static object CalculateVersion(this ICakeContext context, string path)
        {
            var curr = new DirectoryInfo(path);

            while (curr != null && !Directory.Exists(Path.Combine(curr.FullName, ".git")))
            {
                curr = curr.Parent;
            }

            if (curr == null)
            {
                Console.WriteLine("not a git repository");
                return null;
            }

            Repository r = new Repository(curr.FullName);

            var query = from t in r.Tags
                let v = ParseVersion(t.FriendlyName)
                where v != null
                orderby v descending
                select new
                {
                    Tag = t,
                    Version = v
                };

            var latest = query.FirstOrDefault();

            var q = from t in r.Tags
                let commit = t.PeeledTarget
                where commit != null
                select new
                {
                    Commit = commit,
                    Tag = t
                };

            var lookup = q.ToLookup(x => x.Commit.Id, x => x.Tag);

            var count = 0;
            var version = new Version("0.0.0");
            foreach (var commit in r.Commits)
            {
                var t = lookup[commit.Id];
                var tag = t.FirstOrDefault()?.FriendlyName.TrimStart('v', 'V');

                if (!string.IsNullOrEmpty(tag) && Version.TryParse(tag, out var v))
                {
                    version = v;
                    break;
                }
                count++;
            }
            var major = version.Major;
            var minor = version.Minor;
            var patch = version.Build;
            var preReleaseTag = CalculatePreReleaseTag(r);

            // If nothing bumped it from the initial version
            // set the version to 0.1.0
            if (major == 0 & minor == 0 && patch == 0)
            {
                minor = 1;
            }
            else if (count > 0)
            {
                patch++;
            }


            var response = new
            {
                SemVer = SemVer(major, minor, patch, preReleaseTag, count),
                FullSemVer = FullSemVer(major, minor, patch, preReleaseTag, count),
                NuGetVersion = NuGetVersion(major, minor, patch, preReleaseTag, count),
                Major = major,
                Minor = minor,
                Patch = patch,
                Commits = count,
                PreReleaseTag = preReleaseTag
            };

            return response;
        }

        private static string FullSemVer(int major, int minor, int patch, string preReleaseTag, int commits)
        {
            if (!string.IsNullOrEmpty(preReleaseTag))
            {
                return $"{major}.{minor}.{patch}-{preReleaseTag}.{commits}";
            }

            if (commits > 0)
            {
                return $"{major}.{minor}.{patch}+{commits}";
            }

            return $"{major}.{minor}.{patch}";
        }

        private static string SemVer(int major, int minor, int patch, string preReleaseTag, int commits)
        {
            if (!string.IsNullOrEmpty(preReleaseTag))
            {
                return $"{major}.{minor}.{patch}-{preReleaseTag}.{commits}";
            }

            return $"{major}.{minor}.{patch}";
        }

        private static string NuGetVersion(int major, int minor, int patch, string preReleaseTag, int commits)
        {
            if (!string.IsNullOrEmpty(preReleaseTag))
            {
                preReleaseTag = preReleaseTag.Substring(0, Math.Min(16, preReleaseTag.Length)).ToLower();
                return $"{major}.{minor}.{patch}-{preReleaseTag}{commits:D4}";
            }

            return $"{major}.{minor}.{patch}";
        }

        private static string CalculatePreReleaseTag(Repository r)
        {
            var preReleaseTag = "";
            var branch = r.Head.FriendlyName;
            if (branch != "master")
            {
                var idx = branch.LastIndexOf('/');
                if (idx > 0)
                {
                    preReleaseTag = branch.Substring(idx + 1);
                }
                preReleaseTag = preReleaseTag.Replace('_', '-').Substring(0, Math.Min(30, preReleaseTag.Length));
            }

            return preReleaseTag;
        }
    }
}
