# plugins

Module build output lands here automatically via `src/Modules/Directory.Build.targets`
(any module project with `<IsPluginModule>true</IsPluginModule>`).

The Host scans this folder at startup (`ModuleLoader` in YG.BuildingBlocks.Modules).

Contents are generated - do not edit by hand. When renaming or removing a module
project, delete its stale subfolder here manually.
