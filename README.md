# Kubectl for PowerShell

[![powershellgallery](https://img.shields.io/powershellgallery/v/Kubectl.svg)](https://www.powershellgallery.com/packages/Kubectl)
[![downloads](https://img.shields.io/powershellgallery/dt/Kubectl.svg?label=downloads)](https://www.powershellgallery.com/packages/Kubectl)
[![builds](https://img.shields.io/vso/build/felixfbecker/ac9f86f8-64e9-4d02-934f-f0725c27e283/3.svg)](https://felixfbecker.visualstudio.com/PSKubectl/_build/latest?definitionId=3&branch=master)
![powershell: >=6.1.0-preview.3](https://img.shields.io/badge/powershell-%3E%3D6.1.0--preview.3-orange.svg)
[![codecov](https://codecov.io/gh/felixfbecker/PSKubectl/branch/master/graph/badge.svg)](https://codecov.io/gh/felixfbecker/PSKubectl)

🚧 Work in progress 🚧 

`kubectl` implemented as PowerShell Cmdlets, giving you native PowerShell object output, tab completion and error handling - while mirroring the default output formatting of `kubectl`.


Runs in PowerShell Core on macOS, Linux and Windows. Note: Windows PowerShell v5 is not supported. If you are on Windows, you need to install PowerShell Core.
You must use PowerShell Core `v6.1.0-preview.3` or newer.

## Authentication

You currently need to run `kubectl proxy` in the background and connect to that as auth providers are not implemented yet.

You can configure the default host to use for all cmdlets by adding this to your profile.ps1:

```powershell
$PSDefaultParameterValues['*-Kube*:ApiEndPoint'] = 'http://127.0.0.1:8001'
```

## Features

*   `Get-KubePod`
*   `Get-KubeLog`

## Development

Run `./build.ps1` to build.

`./load.ps1` will build and load the module into a new shell. Run `exit` and rerun `./load.ps1` to reload the module.
