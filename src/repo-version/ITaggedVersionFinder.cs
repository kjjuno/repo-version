using LibGit2Sharp;

namespace repo_version
{
    public interface ITaggedVersionFinder
    {
        RepoVersion GetLastTaggedVersion(Repository repo, out int commitsSinceTag);
    }
}
