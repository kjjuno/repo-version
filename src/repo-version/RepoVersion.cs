using System;
using System.Text.RegularExpressions;

namespace repo_version
{
    class RepoVersion : IComparable
    {
        private string _label;

        public string SemVer
        {
            get
            {
                if (!string.IsNullOrEmpty(Label))
                {
                    return $"{Major}.{Minor}.{Patch}.{Commits}-{Label}{DirtyString()}";
                }

                return $"{Major}.{Minor}.{Patch}.{Commits}{DirtyString()}";
            }
        }
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int Commits { get; set; }
        public bool IsDirty { get; set; }
        public string Label
        {
            get => _label ?? string.Empty;
            set => _label = value;
        }

        public override string ToString()
        {
            return SemVer;
        }

        private string DirtyString()
        {
            return IsDirty
                ? "+1"
                : "";
        }
        
        private static int ParseInt(string input)
        {
            var val = !string.IsNullOrEmpty(input) ? input : "0";

            return int.Parse(val);
        }

        public static bool TryParse(string input, out RepoVersion version)
        {
            version = null;
            var match = Regex.Match(input, @"(?<major>\d+)(?:\.(?<minor>\d+)(?:\.(?<patch>\d+)(?:\.(?<commits>\d+))?)?)?(?:-(?<tag>.+))?");

            if (!match.Success)
            {
                return false;
            }

            version = new RepoVersion();
            version.Major = ParseInt(match.Groups["major"].Value);
            version.Minor = ParseInt(match.Groups["minor"].Value);
            version.Patch = ParseInt(match.Groups["patch"].Value);
            version.Commits = ParseInt(match.Groups["commits"].Value);
            version.Label = match.Groups["tag"].Value ?? "";

            return true;
        }

        public int CompareTo(object obj)
        {
            var other = obj as RepoVersion;

            if (other == null)
            {
                return 1;
            }

            int major = this.Major.CompareTo(other.Major);

            if (major != 0)
            {
                return major;
            }

            int minor = this.Minor.CompareTo(other.Minor);

            if (minor != 0)
            {
                return minor;
            }

            int patch = this.Patch.CompareTo(other.Patch);

            if (patch != 0)
            {
                return patch;
            }

            int commits = this.Commits.CompareTo(other.Commits);

            if (commits != 0)
            {
                return commits;
            }

            return this.Label.CompareTo(other.Label);
        }
    }
}
