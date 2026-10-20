# RimRef Local

[RimRef](https://github.com/krafs/RimRef) (`Krafs.Rimworld.Ref`) gives your mod reference assemblies: the game's signatures without its code. That's all the compiler needs, but it means your IDE shows empty method bodies when you go to the definition of a game method.

RimRef Local fixes that on your machine. In Debug builds, if your installed RimWorld matches your RimRef version, your project compiles against the game's own assemblies instead, so your IDE shows the game's real code. Release builds, and machines without RimWorld such as CI, keep using RimRef.

It works with RimWorld 1.0 to 1.7.

## Getting started
Next to your RimRef `PackageReference`, add:

```xml
<PackageReference Include="Krafs.Rimworld.Ref.Local" Version="1.0.0" PrivateAssets="all" />
```

Or skip the package. It's a single file: save [`Krafs.Rimworld.Ref.Local.targets`](https://raw.githubusercontent.com/krafs/RimRefLocal/main/src/build/Krafs.Rimworld.Ref.Local.targets) as `Directory.Build.targets` next to your `.csproj` or in any parent folder. Already have one? Save it under its own name and add `<Import Project="Krafs.Rimworld.Ref.Local.targets" />` to yours.

Your build output tells you whether or not your RimWorld was used, and why. On the command line, use `dotnet build -tl:off`.

## Finding RimWorld
RimWorld is found in this order:

1. `RimWorldDirectory`, if you set it in your project, a `Directory.Build.props` or an environment variable.
2. Your RimWorld folder, if your project is inside it, such as in `RimWorld/Mods/MyMod`.
3. Your Steam libraries.

## Settings
| Property | Default | |
|---|---|---|
| `RimWorldDirectory` | | Your RimWorld folder, if it isn't found as above. |
| `UseLocalRimWorldAssemblies` | `true` in Debug | `false` turns it off, `true` turns it on in any configuration. |
| `UseStrictRimWorldVersionComparison` | `true` | `false` also uses your RimWorld when only the first two parts of the version match, like 1.6.4850 and 1.6.4871. |

Turning off strict comparison is convenient when Steam has updated the game but you haven't updated RimRef yet. Your Debug and Release builds may then see different game code.
