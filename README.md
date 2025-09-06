# MetaNet - .NET project tools center

This repository contains MetaNet, a small console utility that orchestrates useful .NET tooling (for example, it can run Snitch to detect outdated packages).

This guide explains how to make MetaNet a dotnet tool, so it can be installed globally (similar to how Snitch is installed and invoked as a dotnet tool).

Note: The commands below work with .NET SDK 8 or newer; the project targets net9.0.

## 1. Prepare the project for packing as a dotnet tool

To publish/install as a dotnet tool, the NuGet package must be marked as a tool and provide a command name.

Edit MetaNet.csproj and add the following properties inside a PropertyGroup:

```
<PropertyGroup>
  <!-- Mark this package as a dotnet tool -->
  <PackAsTool>true</PackAsTool>

  <!-- The command users will run: `dotnet metanet` -->
  <ToolCommandName>metanet</ToolCommandName>

  <!-- Package metadata (adjust as you prefer) -->
  <PackageId>MetaNet</PackageId>
  <Version>0.1.0</Version>
  <Authors>Your Name or Org</Authors>
  <Description>MetaNet - .NET project tools center</Description>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
  <RepositoryUrl>https://github.com/your-org-or-user/MetaNet</RepositoryUrl>
</PropertyGroup>
```

Tip: If you don’t want to change the repository source yet, you can also pass many of these values on the command line during packing (e.g., `/p:PackAsTool=true /p:ToolCommandName=metanet`).

## 2. Build and pack the tool

Create a Release build and pack to a NuGet package (.nupkg):

```
dotnet clean

dotnet pack -c Release
```

After packing, you should see something like:

- bin/Release/MetaNet.<version>.nupkg

If you didn’t add metadata into the project file, you can pack with properties on the command line:

```
dotnet pack -c Release \
  /p:PackAsTool=true \
  /p:ToolCommandName=metanet \
  /p:PackageId=MetaNet \
  /p:Version=0.1.1
```

## 3. Install the tool globally from a local package

You can install the tool globally from the generated .nupkg (handy for testing before publishing):

Option A: Use the directory where the nupkg is located as an additional source:

```
# From the repository root (adjust path if needed)
dotnet tool install -g MetaNet --add-source .\bin\Release
```

Option B: Copy the nupkg into a dedicated local folder and use that folder:

```
mkdir .\nupkg
copy .\bin\Release\MetaNet.0.1.0.nupkg .\nupkg\

dotnet tool install -g MetaNet --add-source .\nupkg
```

When successful, you can run the tool as:

```
dotnet metanet
```

Or, because the tool exposes a command, you might also invoke it directly as `metanet` if your global tools path is on PATH (dotnet still prefers `dotnet <command>` style).

## 4. Update and uninstall

- Update to the latest version:

```
dotnet tool update -g MetaNet
```

- Uninstall globally:

```
dotnet tool uninstall -g MetaNet
```

- List installed tools:

```
dotnet tool list -g
```

## 5. Publish to a feed (NuGet.org or private) and install

If you want to distribute MetaNet broadly, push the package to a feed and install from there.

### 5.1. Publish to NuGet.org

Prerequisites:
- Create an API key at https://www.nuget.org/account/apikeys

Commands:

```
# Pack (if not already packed)
dotnet pack -c Release /p:PackAsTool=true /p:ToolCommandName=metanet /p:PackageId=MetaNet /p:Version=0.1.0

# Push the package (adjust version)
dotnet nuget push .\bin\Release\MetaNet.0.1.0.nupkg \
  --api-key <YOUR_NUGET_API_KEY> \
  --source https://api.nuget.org/v3/index.json
```

Users can then install with:

```
dotnet tool install -g MetaNet
```

### 5.2. Publish to a private feed (e.g., Azure Artifacts, GitHub Packages, local server)

- Configure credentials for the feed as required by your provider.
- Push the .nupkg to that feed.
- Install with:

```
dotnet tool install -g MetaNet --add-source <YOUR_FEED_SOURCE_URL_OR_DIR>
```

## 6. Troubleshooting

- Command not found after installation on Windows:
  - Try starting a new terminal so the PATH updates.
  - You can always invoke via `dotnet metanet` which bypasses PATH issues of the tool shim.

- Installation fails due to version conflicts:
  - Ensure you’re installing the same version you packed.
  - Try `dotnet tool uninstall -g MetaNet` first, then install again.

- Pack fails because metadata is missing:
  - Make sure `PackAsTool` and `ToolCommandName` are provided, and `PackageId`/`Version` are set either in the csproj or via `/p:` arguments.

## 7. Example end-to-end (local install)

```
REM 1. (Optional) set properties via CLI instead of editing the csproj
REM    or add them to the csproj as shown above

dotnet clean

dotnet pack -c Release \
  /p:PackAsTool=true \
  /p:ToolCommandName=metanet \
  /p:PackageId=MetaNet \
  /p:Version=0.1.0

REM 2. Install globally from local output directory

dotnet tool install -g MetaNet --add-source .\bin\Release

REM 3. Run the tool

metanet
```

With these steps, MetaNet behaves as a globally installed dotnet tool, similar to how the application itself can install and run Snitch.