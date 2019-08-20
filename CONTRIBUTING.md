# Contributing

All contributions are welcome, no matter how small. If you are new to github and are looking
for an easy way to contribute, documentation is always appreciated.

Another great way to contribute is simply to write up issues. If you are aware of any defects,
or would really like to see a new feature, please create an issue.

If you have a flair for testing or code clean up, this project is still in its infancy and
does not have a testing framework established. I am sure much of the code will need to be
refactored to make it easier to test.

## Development Requirements

1. .NET Core SDK 2.1 or greater
2. mono (MacOS and Linux only)

## How to build

This project uses a cake build script. From a bash shell run the following command


```bash
./build.sh
```

optionally you can provide a specific target


```bash
./build.sh --target <target>
```

Available Targets:

| Target    | Description                                |
| --------- | ------------------------------------------ |
| Pack      | (Default) Creates NuGet package            |
| Publish   | Publishes nuget package to nuget.org       |
| Install   | Installs the repo-version tool from source |
