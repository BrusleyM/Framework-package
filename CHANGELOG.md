# Changelog

## [Unreleased] - 2.0.0-preview.1

### Breaking
| Old assembly | New assembly |
|---|---|
| `Framework` (root, held Outline and SnapObject) | `Framework.Tools` and `Framework.Outline` |
| `Audio` | `Framework.Audio` |
| `Task` | `Framework.Tasks` |
| `Sequential` | `Framework.Sequencing.Unity` |
| `Framework.Extensions` | removed |

Assembly references by name must be updated; references by GUID keep working except the removed `Framework.Extensions`.
`GameObjectExtension.SetLayerRecursively` and `RevertLayerRecursively` were removed.

### Changed
- Assemblies renamed under the `Framework.*` prefix and re-layered: `Framework.Audio`, `Framework.CheckPoints`, `Framework.Outline`, `Framework.Tasks`, `Framework.Tools`, `Framework.Sequencing.Unity`.
- `Framework.Outline` is now its own assembly instead of living in a catch-all root assembly.
- The manifest no longer advertises a Package Manager sample. The `Samples` folder holds the Test scene for developing this package and is not importable through Package Manager.

### Changed (outline)
- Outline shaders are URP HLSL and SRP Batcher compatible; the package now depends on the Universal Render Pipeline.
- Outline no longer runs a per-frame coroutine, uses shared materials instead of instancing per toggle, raises `Disabled`, and resets its static mesh registry when entering play mode.

### Removed
- `GameObjectExtension` layer helpers (unused, held static state).
- Personal email from the package manifest.

### Added
- MIT license.
