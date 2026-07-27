---
title: Build Setup
---

import AsciinemaPlayer from '@site/src/components/AsciinemaPlayer';

After [installing the Fallout global tool](01-installation.md), you can call it from anywhere on your machine to set up a new build:

```powershell
# terminal-command
fallout :setup
```

:::tip
Preferably, you should run the setup from inside an existing repository. Fallout will search for the next upwards `.git` or `.svn` directory to determine the _build root directory_. If neither is found, it will use the current directory. You can also pass the `--root` parameter to specify that the current directory should be used as a root directory.
:::

During the setup, you'll be asked several questions to configure your build to your preferences:

<AsciinemaPlayer
    src="/casts/setup.cast"
    idleTimeLimit={2}
    poster="npt:5.715135"
    preload={true}
    terminalFontFamily="'JetBrains Mono', Consolas, Menlo, 'Bitstream Vera Sans Mono', monospace"
    loop={true}/>

**Congratulations!** 🥳 Your first build has now been set up, and you can [run the build](03-execution.md) with the default implementation!

## Effective Changes

The setup will create a number of files in your repository and – if you've chosen so – add the build project to your solution file. Below, you can examine the structure of added files and what they are used for:

```bash
<root-directory>
├── .config
│   └── dotnet-tools.json           # Local tool manifest pinning Fallout.GlobalTool
│
├── .fallout                        # Root directory marker
│   ├── build.schema.json           # Build schema file
│   └── parameters.json             # Default parameters file
│
├── build
│   ├── _build.csproj               # Build project file
│   ├── Build.cs                    # Default build implementation
│   ├── Directory.Build.props       # MSBuild stop files
│   └── Directory.Build.targets
│
├── build.ps1                       # Windows/PowerShell bootstrapping
└── build.sh                        # Linux/Shell bootstrapping
```

:::note
The two thin bootstrappers (`build.ps1` and `build.sh`) provision the .NET SDK locally when it's not on `PATH`, then run `dotnet tool restore` and `dotnet fallout "$@"`. They're optional once you have a global `dotnet` install and have run `dotnet tool restore` at least once — but they're the safest way to run the build in CI and on a freshly-cloned machine. The `.config/dotnet-tools.json` manifest pins the exact `Fallout.GlobalTool` version your build expects.
:::

## Project Structure

While you can enjoy writing most build-relevant logic inside your build console applications, there is still a large number of files involved in the general process of build automation. Fallout organizes these files in different folders as linked files in the build project for you:

<Tabs>
  <TabItem value="config" label="Config" default>

```powershell
<root-directory>
├── .config
│   └── dotnet-tools.json    # Local tool manifest (Fallout.GlobalTool pin)
│
├── .fallout
│   ├── parameters.json      # Parameters files
│   └── parameters.*.json
│
├── global.json              # SDK version
├── nuget.config             # NuGet feeds configuration
└── version.json             # Nerdbank GitVersioning configuration
```

  </TabItem>
  <TabItem value="ci" label="CI/CD">

```powershell
<root-directory>
├── .github
│   └── workflows            # GitHub Actions
│       └── *.yml
│
├── .teamcity                # TeamCity
│   └── settings.kts
│
├── .gitlab-ci.yml           # GitLab CI
├── .space.kts               # JetBrains Space
├── .travis.yml              # Travis CI
├── appveyor.yml             # AppVeyor
├── appveyor.*.yml
├── azure-pipelines.yml      # Azure Pipelines
├── azure-pipelines.*.yml
└── bitrise.yml              # Bitrise
```

  </TabItem>
  <TabItem value="bootstrappers" label="Bootstrappers">

```powershell
<root-directory>
├── build.ps1                # Windows/PowerShell — thin shim, calls dotnet fallout
└── build.sh                 # Linux/Shell — thin shim, calls dotnet fallout
```

  </TabItem>
  <TabItem value="auto-imports" label="MSBuild&nbsp;Auto&#8209;Imports">

```powershell
<root-directory>
└── **
    ├── Directory.Build.props
    └── Directory.Build.targets
```

  </TabItem>
</Tabs>

:::info
You can deactivate linking of the above files by removing the `FalloutRootDirectory` and `FalloutScriptDirectory` properties from the build project file. (Legacy `NukeRootDirectory` / `NukeScriptDirectory` properties are still recognized for migration but should be renamed.)

```xml title="_build.csproj"
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    // highlight-start
    {/* <FalloutRootDirectory>..</FalloutRootDirectory> */}
    {/* <FalloutScriptDirectory>..</FalloutScriptDirectory> */}
    // highlight-end
  </PropertyGroup>

</Project>
```

:::

[^1]: Interface default members behave like explicit interface implementations, which means that to access their members, the `this` reference must be cast explicitly to the interface type. For instance, `((IComponent)this).Target`.
