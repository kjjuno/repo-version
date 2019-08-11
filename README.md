# repo-version

Automatic versioning for git repositories based tags, and the number of commits since the last tag.

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

## Quick Start

Install

```bash
dotnet tool install -g repo-version
```

Update

```bash
dotnet tool update -g repo-version
```

You need to be somewhere within a git repository to run `repo-version`. Alternatively, you can provide a path as an argument.

Let's say you have branched off of master at tag 1.2.2.1 and created a branch named `bugfix/fix-null-reference`.
During your development you currently have 2 commits on your feature branch. And you have some local changes that you have not yet committed.

run `repo-version` to calculate the current version

```bash
repo-version
1.2.3.2-fix-null-reference+1
```

or for more verbose output

```bash
repo-version -o json
{
    "SemVer": "1.2.3.2-fix-null-reference+1",
    "Major": "1",
    "Minor": "2",
    "Patch": "3",
    "Commits": "2",
    "IsDirty": true,
    "Label": "fix-null-reference"
}

```

Because you have local changes that have not yet been committed the version is considered dirty and appends the `+1` to the calculated version to indicate that there are additional changes.

For this example we have decided that we want to keep those changes, so we commit those to the git history.

```bash
repo-version
1.2.3.3-fix-null-reference
```

Note that the number of commits went up, and the dirty indicator was dropped.

Now, let's say that your branch is ready to be merged, and you use a merge commit strategy. This will add 1 more commit.

```bash
git checkout master
git merge bugfix/fix-null-reference --no-ff
```

Now on the master branch we run `repo-version` again.

```bash
$ repo-version
1.2.3.4
```

## Tagging

Tags indicate to `repo-version` that the current patch is official, and versions should begin counting toward the next patch.
It will be typical to branch from master, and merge back to master, and tag each one of those merges with the official version.

So, with our example above let's say you are ready to complete the 1.2.3.x release. We accomplish that with a git tag.
`repo-version` has built in support to help with this.

```bash
repo-version tag
```

This will apply the current version as a tag. The next commit will be be automatically bumped to `1.2.4.1`

## Versioning Rules

Each of the 4 numbers in the version mean something specific. This slightly extends the recommendation from semver.

{major}.{minor}.{patch}.{commits}-{label}

The `label` indicates that the version is a pre-release version. This should be ommited on
full release versions.

By default `repo-version` will begin a new repository with a version of `0.1.0.x`, where x is the number of commits in the git history.
Applying a release version tag to the repository will reset the `commits` and increment `patch`.

### Pre-Release Labels

pre-release version tags will not increase the version numbers in any way, however the `label` will persist on main-line branches until a new tag changes or drops the label.
Take a look at the [Configuration](#Configuration) section for more details on controlling the default label.

### Incrementing Major and Minor Versions

`repo-version` will respect version tags. So, any manually applied tag will increase the version
to whatever the tag says it is. However, it is recommended that the `major` and `minor` versions
be controlled in the `repo-version.json` file. (see [Configuration](#Configuration)).

While you are free to modify `repo-version.json` directly, there are convenience commands provided to assist with this task.

Increment `minor` version:

```bash
repo-version minor
```

This will increment the minor version by 1 in the config file.

Increment `major` version:

```bash
repo-version major
```

This will increment the `major` version by 1, and reset the `minor` version to 0 in the config file.

### Configuration

example repo-version.json located at the root of your git repository

```json
{
  "major": 0,
  "minor": 1,
  "branches": [
    {
      "regex": "^master$",
      "defaultLabel": "",
      "mainline": true
    },
    {
      "regex": "^support[/-].*$",
      "defaultLabel": "",
      "mainline": true
    },
    {
      "regex": ".+",
      "defaultLabel": "{BranchName}",
      "mainline": false
    }
  ]
}
```

This file can be generated by running the following command

```bash
repo-version init
```

The `branches` section is an ordered list of branch configs.
`repo-version` will do a Regex match against each branch config to find the rules that
should apply to the current branch. The first rule that matches will be used.
The matching rule will be used to determine the `label`.

The `defaultLabel` indicates which `label` should be applied to commits after a release version.
So, for example:

You have just released `v3.2.4.9` of a nuget package you maintain. Your `repo-version.json` looks like this:

```json
{
  "major": 3,
  "minor": 2,
  "branches": [
    {
      "regex": "^master$",
      "defaultLabel": "alpha",
      "mainline": true
    },
    {
      "regex": "^support[/-].*$",
      "defaultLabel": "",
      "mainline": true
    },
    {
      "regex": ".+",
      "defaultLabel": "{BranchName}",
      "mainline": false
    }
  ]
}
```

This configuration specifies that by default the `master` branch should have the `alpha` label.
The next commit after the `3.2.4.9` tag would be calculated to be `3.2.5.1-alpha`.
The `alpha` label will persist until the repository is tagged with a different `label`.

Example:

The last tag on `master` is `3.2.5.67-alpha`. There have been 34 additional commits and now the project is now ready to be promoted to a `beta` status. 

Tag the repository with the current version, and change the label to `beta`

```bash
repo-version tag -l beta
```

This will tag the repository with a `beta` label and will produce this version `3.2.5.101-beta`.

It should be noted that you cannot apply multiple tags to the same commit. So, you will need to make at least an empty commit in order to change the label. 

This strategy should be followed to specify `alpha`, `beta`, `rc`, etc... labels until you are ready to drop the pre-release label all-together.
So, for our example, if there have been 21 additional commits since the beta tag, and we are ready
to release this version we would run the following command.


```bash
repo-version tag -l ""
```

This will apply the `3.2.5.122` tag, and that will complete the `3.2.5.x` version.
If your branch config sepcifies `alpha` as the `defaultLabel` the next version would be `3.2.6.x-alpha`, where x is the number of commits since the `3.2.5.122` tag.

## Cake.RepoVersion

This is a simple wrapper around the repo-version dotnet tool. To use this in Cake you will need to include it as an addin.

```bash
#addin nuget:?package=Cake.RepoVersion&version=<version>
#addin nuget:?package=Newtonsoft.Json&version=11.0.2
```

It is important that you also include the addin for `Newtonsoft.Json` and it must be `11.0.2`

```bash
var repoVersion = RepoVersion();
Information(repoVersion.SemVer);
```

## Roadmap

|    | Version | Description                                                                           |
| -- | ------- | ------------------------------------------------------------------------------------- |
|    | 0.1     | Proof of concept phase. General algorithm works, but still working out the interface  |
| -> | 0.2     | Committed to keep the interface from 0.1. Squashing bugs, and working on docs         |
|    | 0.3     | Interface might change from 0.2. It's unclear yet if this will be compatible with 0.2 |
