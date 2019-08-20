using System;
using System.IO;
using LibGit2Sharp;
using Moq;
using NUnit.Framework;
using repo_version;

namespace Tests
{
    public class Tests
    {
        Mock<IGitFolderFinder> folderFinder;
        Mock<ITaggedVersionFinder> tagFinder;
        Mock<ILabelCalculator> labelCalculator;
        VersionCalculator versionCalculator;
        
        [SetUp]
        public void Setup()
        {
            folderFinder = new Mock<IGitFolderFinder>();
            tagFinder = new Mock<ITaggedVersionFinder>();
            labelCalculator = new Mock<ILabelCalculator>();
            versionCalculator = new VersionCalculator(folderFinder.Object, tagFinder.Object, labelCalculator.Object);
        }

        [Test]
        public void Test1()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(path);
            Repository.Init(path);
            folderFinder.Setup(f => f.FindGitFolder(It.IsAny<string>()))
                .Returns(path);
            var commitsSinceTag = 34;
            tagFinder.Setup(f => f.GetLastTaggedVersion(It.IsAny<IRepository>(), out commitsSinceTag))
                .Returns(new RepoVersion
                {
                    Major = 1,
                    Minor = 2,
                    Patch = 3,
                    Commits = 4,
                    Label = ""
                });
            var options = Options.Parse(new string[0]);
            var actual = versionCalculator.CalculateVersion(options);

            Directory.Delete(path, true);

            var expected = new RepoVersion
            {
                Major = 1,
                Minor = 2,
                Patch = 4,
                Commits = commitsSinceTag,
                Label = ""
            };
            Assert.AreEqual(expected, actual);
        }
    }
}
