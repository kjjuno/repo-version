using System;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace repo_version
{
    public class LabelCalculator : ILabelCalculator
    {
        public void CalculateLabel(Repository repo, Configuration config, out string label, out bool isMainline)
        {
            bool found = false;
            label = "";
            isMainline = false;
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
        }
    }
}
