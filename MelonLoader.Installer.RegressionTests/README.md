# Mac installation regression checks

This dependency-free console harness references the actual installer project. It checks release and nightly macOS asset selection, empty/incompatible version selection, installation state notifications and their XAML bindings, post-operation guidance, and literal shell argument handling. On Unix, it executes a temporary `printf` wrapper; it does not launch or modify any game or Steam setting.

With the .NET 9 SDK:

```sh
dotnet run --project MelonLoader.Installer.RegressionTests -c Release -p:RuntimeIdentifiers=osx-arm64
```

Use the appropriate RID for your host (for example `osx-x64`). A failure throws and exits nonzero. This checks production logic and binding declarations; it does not replace a visual Avalonia check or a real Steam/game launch.
