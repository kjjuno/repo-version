using LibGit2Sharp;

namespace repo_version
{
    public interface ILabelCalculator
    {
        void CalculateLabel(Repository repo, Configuration config, out string label, out bool isMainline);
    }
}
