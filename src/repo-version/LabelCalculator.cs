using System;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace repo_version
{
    class LabelCalculator
    {
        public void CalculateLabel(Repository repo, Configuration config, out string label, out bool isMainline)
        {
            bool found = false;
            label = "";
            isMainline = false;
            var branch = repo.Head.FriendlyName;

            // TODO: This needs to be reworked to more gracefully handle pull reqeusts
            // from multiple server types.
            if (branch == "(no branch)")
            {
                var appveyor = (Environment.GetEnvironmentVariable("APPVEYOR") ?? "false").ToLower();
                var gitBranch = Environment.GetEnvironmentVariable("Git_Branch");

                if (appveyor == "true")
                {
                    var pullRequestNumber = Environment.GetEnvironmentVariable("APPVEYOR_PULL_REQUEST_NUMBER");
                    var pullRequestBranch = Environment.GetEnvironmentVariable("APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH");

                    branch = Environment.GetEnvironmentVariable("APPVEYOR_REPO_BRANCH");

                    if (!string.IsNullOrEmpty(pullRequestNumber) && !string.IsNullOrEmpty(pullRequestBranch))
                    {
                        label = $"pull-request-{pullRequestNumber}";
                    }
                }
                else if (!string.IsNullOrEmpty(gitBranch))
                {
                    branch = gitBranch;
                }
            }

            var pullRequestMatch = Regex.Match(branch, @"(?<bitbucket>refs/pull-requests/(?<prnumber>\d+)/merge)");
            if (pullRequestMatch.Success)
            {
                if (pullRequestMatch.Groups["bitbucket"].Success)
                {
                    var pullRequestNumber = pullRequestMatch.Groups["prnumber"].Value;
                    label = $"pull-request-{pullRequestNumber}";
                }
            }


            if (string.IsNullOrEmpty(label))
            {
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
                .TrimEnd('-')
                .TrimEnd('.');
        }
    }
}
