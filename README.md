# Kubectl for PowerShell

[![powershellgallery](https://img.shields.io/powershellgallery/v/kubectl.svg)](https://www.powershellgallery.com/packages/kubectl)
[![downloads](https://img.shields.io/powershellgallery/dt/kubectl.svg?label=downloads)](https://www.powershellgallery.com/packages/kubectl)
[![codecov](https://codecov.io/gh/felixfbecker/ps-kubectl/branch/master/graph/badge.svg)](https://codecov.io/gh/felixfbecker/ps-kubectl)
[![windows build](https://img.shields.io/appveyor/ci/felixfbecker/ps-kubectl/master.svg?label=windows+build)](https://ci.appveyor.com/project/felixfbecker/ps-kubectl)
[![macos/linux build](https://img.shields.io/travis/felixfbecker/ps-kubectl/master.svg?label=macos/linux+build)](https://travis-ci.org/felixfbecker/ps-kubectl)

`kubectl` implemented as PowerShell Cmdlets, giving you native PowerShell object output, tab completion and error handling. Work in progress 🚧

You currently need to run `kubectl proxy` in the background and connect to that as auth providers are not implemented yet.

You can configure the default host to use for all cmdlets by adding this to your profile.ps1:

```powershell
$PSDefaultParameterValues['*-Kube*:ServerHost'] = 'http://localhost:8001'
```

## Features

*   `Get-KubePod(s)`
*   `Get-KubePodLog`

## Build

```
dotnet build --configuration Release
```
