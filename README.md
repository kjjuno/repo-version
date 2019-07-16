# repo-version
automatic versioning for git repositories

This repository is in the very infant stages. The design is still being considered and is very likely to change.

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

## Versioning Rules

major.minor.patch.commits-since-last-tag

master: 0.1.0.0
branch: 0.1.1-branch-name-abbrev0001 or 0.1.1-branch-name-abbrev.1
    where the final number is the number of commits since the tag
merge
master: 0.1.1.2 (should tag at this point)

1.0.0 features and assumptions

1. master is main branch
2. all branches start from master, and are merged back to master.
3. major and minor revisions controlled by config file
4. patch and commits are controlled by commits since tag.
5. only need current branch to calculate version (no more bad versions without master)

## Language choice
I would like to implement this using something cross platform. It MUST be available to mac, windows, and linux as I will need it on all of those operating systems. It should be easy to install, and should feel native.

I am most familiar with .net core, which could be a suitable choice, but I also think that Go would be a great choice.
