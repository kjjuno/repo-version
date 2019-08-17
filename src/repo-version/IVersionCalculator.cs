namespace repo_version
{
    public interface IVersionCalculator
    {
        RepoVersion CalculateVersion(Options options);
    }
}
