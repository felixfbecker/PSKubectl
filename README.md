# Kubectl for PowerShell

[![powershellgallery](https://img.shields.io/powershellgallery/v/PSKubectl.svg)](https://www.powershellgallery.com/packages/PSKubectl)
[![downloads](https://img.shields.io/powershellgallery/dt/PSKubectl.svg?label=downloads)](https://www.powershellgallery.com/packages/PSKubectl)
[![build](https://img.shields.io/travis/felixfbecker/PSKubectl/master.svg)](https://travis-ci.org/felixfbecker/PSKubectl)
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

### App Management

#### `Get-KubeResource`

Equivalent to `kubectl get` and `kubectl describe`.
Just like with those, the first parameter is the kind of resource to get.
The default output formatting mirrors the tabular output of `kubectl get`, but you can get all Pod properties from the returned objects.
The `Name` parameter supports wildcard patterns, which can be very convenient to get all pods for a deployment (e.g. `Get-KubePod my-deployment-*`).
It also supports tab-autocompletion.
Pass the namespace with `-Namespace`.

There are also specialised cmdlets for common kinds like `Get-KubePod` and `Get-KubeDeployment`. They work the same way, but without `Kind` parameter.

#### `Get-KubeLog`

Equivalent to `kubectl logs`. Pass `-Follow` to stream logs.  
The cmdlet accepts pipeline input from `Get-KubePod`.

#### `Update-KubeResource`

Equivalent to `kubectl apply`.
Takes Kubernetes objects or YAML file paths as pipeline or parameter input, compares them with the state of that object on the server, generates a three-way patch and sends it to the server.
Supports `-WhatIf` and `-Confirm`.
Prints the updated object returned by the server.

Example:
```powershell
Update-KubeResource *.yml
Get-ChildItem *.yml -Recurse | Update-KubeResource
```

Editing a field before updating:
```powershell
Get-ChildItem *.Deployment.yml -Recurse |
    Get-Content -Raw |
    ConvertFrom-KubeYaml |
    ForEach-Object { $_.Spec.Template.Spec.Containers[0].Image = $newImage; $_ } |
    Update-KubeResource
```

#### `ConvertFrom-KubeYaml`

Takes YAML strings as pipeline input, parses it and returns Kubernetes objects.
The objects are PSObjects with only the properties from the YAML set (important for diffing purposes), but will have the correct type names set for pretty output formatting.
Other cmdlets take these objects as input.

#### `Compare-KubeResource`

Compare two Kubernetes objects and output a JSON Patch.

Example that replicates the logic of `Update-KubeResource`:
```powershell
$modified = Get-Content -Raw deployment.yml | ConvertFrom-KubeYaml
$original = $modified | Get-KubeDeployment
Compare-KubeResource -Original $original -Modified $modified -ThreeWayFromLastApplied -Annotate
```

### Configuration

#### `Get-KubeConfig`

Gets the Kubernetes configuration parsed from `~/.kube/config`.

To get the clusters like `kubectl config get-clusters`, run `(Get-KubeConfig).Clusters`.  
To get all contexts like `kubectl config get-contexts`, run `(Get-KubeConfig).Contexts`.  
To get the current context like `kubectl config current-context`, run `(Get-KubeConfig).CurrentContext`

#### `Set-KubeConfig`

**Warning: This currently deletes unsupported auth configuration!**

A convenience cmdlet to update kubeconfig. Does nothing but serialize a given config object back to YAML and save it back to `~/.kube/config`.

#### `Use-KubeContext`

A convenience cmdlet to update the current context. Supports tab-completion for the context name. Equivalent of `kubectl config use-context`.

## Development

Run `./build.ps1` to build.

`./load.ps1` will build and load the module into a new shell. Run `exit` and rerun `./load.ps1` to reload the module.

### Tests

Tests are written in PowerShell with Pester and run against an actual test Kubernetes cluster, such as Minikube or Docker for Desktop.
If you want to run them locally, make sure to set your kubectl context to a throw-away cluster, and set `$PSBoundParameterValues['*-Kube*:ApiEndPoint']` to point to that cluster.
Then execute `Invoke-Pester ./Tests` to run the tests.
