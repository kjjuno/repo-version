[![Nuget](https://img.shields.io/nuget/v/repo-version?style=plastic)](https://www.nuget.org/packages/repo-version)

# repo-version
Automatic versioning for git repositories based tags, and the number of commits since the last tag.

## Install

```
dotnet tool install -g repo-version
```

## Usage

You need to be somewhere within a git repository to run `repo-version`. Alternatively, you can provide a path as an argument.

Let's say you have branched off of master at tag 1.2.2.1 and created a branch named `feature/fix-null-reference`.
During your development you currently have 3 commits on your feature branch.

```
$ repo-version
1.2.2.3-fix-null-reference
```

or for more verbose output

```
$ repo-version -o json
{
    "SemVer": "1.2.3.3-fix-null-reference",
    "Major": "1",
    "Minor": "2",
    "Patch": "3",
    "Commits": "3",
    "PreReleaseTag": "fix-null-reference"
}

```

Now, let's say that your branch is ready to be merged, and you use a merge commit strategy. This will add 1 more commit.
Now on the master branch we run `repo-version` again.

```
$ repo-version
1.2.3.4
```

When you are ready to finish the 1.2.3.x release you should tag the final commit.

```
git tag $(repo-version)
git push --tags
```

The next commit will be be automatically bumped to `1.2.4.1`

## Why not just use GitVersion?

For years now I have used GitVersion, but I have a few gripes with it. First, I almost always
use GitHubFlow, or at least I tend to branch from master, and merge to master. I find myself
almost always controlling the major/minor revision with the next-version property, and using
tags and commits to control the rest of the versioning. I do not need, and do not want, all of
the other features that GitVersion provides. The extra config options it provides have frequently
led to the inablility to correctly calculate the version during a build on a ci server, or worse,
a VERY long build time where most of it was trying to calcualate the build on a very large repo.
This project aims to acheive the parts of GitVersion that I love, without all of the baggage.
As such, this project should be extremely light weight and opinionated. It will only support the
git workflow that I use. Below are my initial thoughts for the first version.

## 1.0.0 features and assumptions

1. master is main branch
2. all branches start from master, and are merged back to master.
3. major and minor revisions controlled by config file
4. patch and commits are controlled by commits since tag.
5. only need current branch to calculate version (no more bad versions without master)

