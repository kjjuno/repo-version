dotnet publish ./repo-version/repo-version.csproj -c Release -f netcoreapp2.1 -o ../artifacts/linux -r linux-x64
dotnet publish ./repo-version/repo-version.csproj -c Release -f netcoreapp2.1 -o ../artifacts/windows -r win10-x64
dotnet publish ./repo-version/repo-version.csproj -c Release -f netcoreapp2.1 -o ../artifacts/macOS -r osx.10.10-x64