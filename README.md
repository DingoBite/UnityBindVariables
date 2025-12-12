# UnityBindVariables

Simple lightweight reactive variable wrappers for Unity.

## What it is

A small set of wrappers around values that:
- store the current value,
- notify subscribers when the value changes,
- let you build derived values (processed),
- provide the same concept for a dictionary (dict),
- include extension utilities for common scenarios.

This repo contains:
- `Bind` (value + notifications)
- `BindProcessed` (derived/projected value)
- `BindDict` (dictionary + notifications)
- `BindInterfaces` (interfaces)
- `BindExtensions` (extension utilities)
- `BindAssembly.asmdef` (Unity assembly definition)

## Installation

### Option 1. Copy files
Copy the `.cs` files and `BindAssembly.asmdef` into your Unity project, for example:
`Assets/Plugins/UnityBindVariables/`

### Option 2. Git submodule
```bash
git submodule add https://github.com/DingoBite/UnityBindVariables.git Assets/Plugins/UnityBindVariables
