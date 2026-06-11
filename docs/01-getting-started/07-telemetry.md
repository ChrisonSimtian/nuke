---
title: Telemetry
---

:::warning
**Telemetry is currently a no-op in Fallout.** The previous NUKE telemetry endpoint (Azure Application Insights, owned by the original maintainer) is no longer used. The plumbing on this page describes the framework hooks that *would* fire if a Fallout-controlled endpoint were configured. Today, none is — telemetry short-circuits in the static constructor when no `InstrumentationKey` is set.
:::

As an effort to improve Fallout and to provide you with a better, more tailored experience, we include a telemetry feature that collects anonymous usage data and enables us to make more informed decisions for the future.

We want you to be fully aware about telemetry, which is why the global tool will show a disclosure notice on first start. In addition, every build project requires to define a `NukeTelemetryVersion` property:

```xml title="_build.csproj"
<PropertyGroup>
  <NukeTelemetryVersion>1</NukeTelemetryVersion>
</PropertyGroup>
```

We will increase the telemetry version whenever we add or change significant data points. With every version change and  after updating the `Nuke.Common` package, you will be prompted again for confirmation.

## Disclosure

Fallout will display a prompt similar to the following when executing a build project without the `NukeTelemetryVersion` property being set or when executing the global tool for the first time — *once a telemetry endpoint is configured*.

```text
Telemetry v1
------------
Fallout collects anonymous usage data in order to help us improve your experience.
Read more about scope, data points, and opt-out: https://github.com/Fallout-build/Fallout#telemetry
```

Once you confirm the notice, Fallout will either:

- Create an awareness cookie under `~/.fallout/telemetry-awareness/v1` for the respective global tool, or
- Add the `NukeTelemetryVersion` property to the project file.

## Scope

As a global tool and library, Fallout has [multiple events](https://github.com/Fallout-build/Fallout/blob/main/src/Nuke.Build/Telemetry/Telemetry.Events.cs) where telemetry is collected:

- `BuildStarted` – when a build was started
- `TargetSucceeded` – when a target succeeded (only `Restore`, `Compile`, `Test`)
- `BuildSetup` – when setting up a build via `fallout [:setup]`
- `CakeConvert` – when converting Cake files via `fallout :cake-convert`

:::info
Data for `BuildStarted` and `TargetSucceeded` is only collected when `IsServerBuild` returns `true` (i.e., CI build), or the build is invoked via global tool. I.e., a contributor executing `build.ps1` or `build.sh` will not have telemetry enabled unknowingly. Likewise, when a build project targets a higher telemetry version than the installed global tool, the lower version will be used.
:::

## Data Points

The [telemetry data points](https://github.com/Fallout-build/Fallout/blob/main/src/Nuke.Build/Telemetry/Telemetry.Properties.cs) do not collect personal data, such as usernames or email addresses. If we wire up an endpoint, the data will be sent securely to whichever back-end Fallout's maintainers configure — documented here when that happens.

Protecting your privacy is important to us. If you suspect the telemetry plumbing has been re-enabled incorrectly or could collect sensitive data, file an issue on the [Fallout repository](https://github.com/Fallout-build/Fallout/issues).

The telemetry feature collects the following data:

| Version | Data                                                                                      |
|:--------|:------------------------------------------------------------------------------------------|
| All	    | Timestamp of invocation                                                                   |
| All	    | Operating system                                                                          |
| All	    | Version of .NET SDK                                                                       |
| All	    | Repository provider (GitHub, GitLab, Bitbucket, etc.)                                     |
| All	    | Repository Branch (`main`, `develop`, `feature`, `hotfix`, custom)                        |
| All	    | Hashed Repository URL (SHA256; first 6 characters)                                        |
| All	    | Hashed Commit Sha (SHA256; first 6 characters)                                            |
| All	    | Compile time of build project in seconds                                                  |
| All	    | Target framework                                                                          |
| All	    | Version of `Nuke.Common` and `Nuke.GlobalTool`                                            |
| All	    | Host implementation (only non-custom)                                                     |
| All	    | Build type (project/global tool)                                                          |
| All	    | Number of executable targets                                                              |
| All	    | Number of custom extensions                                                               |
| All	    | Number of custom components                                                               |
| All	    | Used configuration generators and build components (only non-custom)                      |
| All	    | Target execution time in seconds (only for targets named _Restore_, _Compile_, or _Test_) |

:::note
Whenever a type does not originate from the `Nuke` namespace, it is replaced with `<External>`.
:::

## How to opt out

The telemetry feature is enabled by default. To opt out, set the `FALLOUT_TELEMETRY_OPTOUT` environment variable to `1` or `true`. The legacy `NUKE_TELEMETRY_OPTOUT` name is still honoured during the 10.x line for backwards compatibility, but will be removed in 11.0.
