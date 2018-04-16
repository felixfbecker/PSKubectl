# Kubectl for PowerShell

[![powershellgallery](https://img.shields.io/powershellgallery/v/kubectl.svg)](https://www.powershellgallery.com/packages/kubectl)
[![downloads](https://img.shields.io/powershellgallery/dt/kubectl.svg?label=downloads)](https://www.powershellgallery.com/packages/kubectl)
[![codecov](https://codecov.io/gh/felixfbecker/ps-kubectl/branch/master/graph/badge.svg)](https://codecov.io/gh/felixfbecker/ps-kubectl)
[![windows build](https://img.shields.io/appveyor/ci/felixfbecker/ps-kubectl/master.svg?label=windows+build)](https://ci.appveyor.com/project/felixfbecker/ps-kubectl)
[![macos/linux build](https://img.shields.io/travis/felixfbecker/ps-kubectl/master.svg?label=macos/linux+build)](https://travis-ci.org/felixfbecker/ps-kubectl)

`kubectl` implemented as PowerShell Cmdlets, giving you native PowerShell object output, tab completion and error handling.

## Getting Started

```powershell
# Install from the PowerShell Gallery
Install-Module kubectl

# Get all pods
Get-KubePods
```

## Features

*   `Get-KubePod(s)`
*   `Get-KubeLog`

## Build

```
dotnet build --configuration Release
```
