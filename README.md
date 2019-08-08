| package | version | downloads |
| ------- | ------ | ---------- |
| repo-version | [![Nuget][repo-version-current-version]][repo-version-nuget] | [![Nuget][repo-version-downloads]][repo-version-nuget] |
| Cake.RepoVersion | [![Nuget][cake-repo-version-current-version]][cake-repo-version-nuget] | [![Nuget][cake-repo-version-downloads]][cake-repo-version-nuget] |

[repo-version-current-version]: https://img.shields.io/nuget/v/repo-version?style=plastic
[repo-version-downloads]: https://img.shields.io/nuget/dt/repo-version?style=plastic
[repo-version-nuget]: https://www.nuget.org/packages/repo-version

[cake-repo-version-current-version]: https://img.shields.io/nuget/v/cake.repoversion?style=plastic
[cake-repo-version-downloads]: https://img.shields.io/nuget/dt/cake.repoversion?style=plastic
[cake-repo-version-nuget]: https://www.nuget.org/packages/cake.repoversion

# repo-version
Automatic versioning for git repositories based tags, and the number of commits since the last tag.

## Install

```
dotnet tool install -g repo-version
```

## Update
```
dotnet tool update -g repo-version
```

## Usage

You need to be somewhere within a git repository to run `repo-version`. Alternatively, you can provide a path as an argument.

Let's say you have branched off of master at tag 1.2.2.1 and created a branch named `feature/fix-null-reference`.
During your development you currently have 3 commits on your feature branch.

```
$ repo-version
1.2.3.3-fix-null-reference
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

## Tagging

Tags indicate to `repo-version` that the current patch is official, and versions should begin counting toward the next patch.
It will be typical to branch from master, and merge back to master, and tag each one of those merges with the official version.

So, with our example above let's say you are ready to complete the 1.2.3.x release. We accomplish that with a git tag.
`repo-version` has built in support to help with this.

```
repo-version tag
```

This will apply the current version as a tag. The next commit will be be automatically bumped to `1.2.4.1`

## Working with repo-version.json
This file should be created at the root of your repository. If none exists the default seetings will be used.
However, this will control the major and minor versions, as well as provide pre-release tags based on branch names.
It is recommended to include the file, if for no other reason than to manipulate the major and minor versions.

There are several commands that will assist you in working with this file. The most important one is `init`

### init
```
repo-version init
```
This will guide you through the initial setup and will produce a file like this at the root of your repository.

repo-version.json
```json
{
  "major": 0,
  "minor": 1,
  "branches": [
    {
      "regex": "^master$",
      "tag": ""
    },
    {
      "regex": "^support[/-].*$",
      "tag": ""
    },
    {
      "regex": ".+",
      "tag": "{BranchName}"
    }
  ]
}
```

The `branches` section is an ordered list of branch configs. When trying to calculate the pre-release tag `repo-version` will do a Regex match against each branch config, and use the first one that it finds.

### major

This will bump the version to the next major version

```
repo-version major
```

### minor
This will bump the version to the next minor version

```
repo-version minor
```
# Cake.RepoVersion

This is a simple wrapper around the repo-version dotnet tool. To use this in Cake you will need to include it as an addin.

```
#addin nuget:?package=Cake.RepoVersion&version=<version>
#addin nuget:?package=Newtonsoft.Json&version=11.0.2
```
It is important that you also include the addin for `Newtonsoft.Json` and it must be `11.0.2`

```
var repoVersion = RepoVersion();
Information(repoVersion.SemVer);
```
# Why not just use GitVersion?

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

# 1.0.0 features and assumptions

1. master is main branch
2. all branches start from master, and are merged back to master.
3. major and minor revisions controlled by config file
4. patch and commits are controlled by commits since tag.
5. only need current branch to calculate version (no more bad versions without master)

# Roadmap

## 0.1
Early stages. The general algorithm for calculating a version works. The api surface is still being designed and is likely to change.

## 0.2
Api surface should be mostly stable. Documentation should be up to date and accurate

