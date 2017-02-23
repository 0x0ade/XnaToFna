# XnaToFna
### Relink C# assemblies from XNA Framework 4.0 to FNA instead
#### MIT-licensed
#### clone recursively
----

#### TL;DR:
Relinker making <sup>an untested bunch of</sup> games using XNA 4.0 run on Linux with FNA<sup>, as long as they don't do ugly stuff and you clean up the mess</sup>.

* [Download release](https://github.com/0x0ade/XnaToFna/releases) OR compile using Visual Studio, MonoDevelop, `xbuild` or something that compiles C# code.
* Put `XnaToFna.exe` into the game directory.
* Put `FNA.dll` into the game directory.
* Run `XnaToFna.exe` with Mono on Linux / macOS or .NET Framework on Windows.
* Keep `XnaToFna.exe` in the game directory because the game now also links to the `XnaToFnaHelper` inside it, whoops.
* Complain about broken paths, unconverted sound / video assets, Windows-specific P/Invokes and unsupported, closed-source native libraries.

----

#### Note: XnaToFnaHelper hasn't been pushed yet.

----

#### How?

It uses [MonoMod](https://github.com/0x0ade/MonoMod/), which uses [Mono.Cecil](https://github.com/jbevain/cecil) under the hood.

#### How exactly?

It sets up the relinking map in MonoMod to relink all `Microsoft.Xna.Framework` references to `FNA` instead.

It can relink from `Microsoft.Xna.Framework.Net` and `Microsoft.Xna.Framework.GamerServices` to either `MonoGame.Framework.Net` or `FNA.Steamworks`, depending on which of those two exists in the game directory.

Finally, it applies some pepper and salt here and there (`XmlIgnore` this, `XnaToFnaHelper.ProxyWindowHandle` that).

#### What's up with XnaToFna-Legacy?

XnaToFna-Legacy was practically a heavily modified fork of MonoMod, meant to relink and only to relink... and fix the paths, which didn't always work.

This rebuilt version *uses* "MonoMod neo", which itself is a rebuilt version of MonoMod. It now supports defining a custom relinker and relinking maps for the default relinker.

Before, most relinker fixes ping-ponged between MonoMod and XnaToFna, while some of them weren't even compatible with each other.

Keeping the actual relinking in MonoMod means there's only one codebase to maintain and only one place where it can screw up.
