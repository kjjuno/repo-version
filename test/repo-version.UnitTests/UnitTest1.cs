using NUnit.Framework;
using repo_version;

namespace Tests
{
    public class Tests
    {
        VersionCalculator versionCalculator;

        [SetUp]
        public void Setup()
        {
            var folderFinder = new GitFolderFinder();
            var tagFinder = new TaggedVersionFinder();
            var labelCalculator = new LabelCalculator();
            versionCalculator = new VersionCalculator(folderFinder, tagFinder, labelCalculator);
        }

        [Test]
        public void Test1()
        {
            var options = Options.Parse(new string[0]);
            var version = versionCalculator.CalculateVersion(options);
            Assert.Pass();
        }
    }
}
