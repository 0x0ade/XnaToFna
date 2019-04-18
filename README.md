# XnaToFna
### Relink C# assemblies from XNA Framework 4.0 to FNA instead
#### zlib-licensed, clone recursively
#### XnaToFna game status tracker: https://github.com/0x0ade/XnaToFna-Tracker
----

#### TL;DR:
Relinker making some games using XNA 4.0 run with FNA. It goes as far as booting some X360 games on Android.

* [Download release](https://github.com/0x0ade/XnaToFna/releases) OR compile using Visual Studio, MonoDevelop, `xbuild` or something that compiles C# code.
* Put `XnaToFna.exe` into the game directory.
* Put `FNA.dll` and the [native libs](http://fna.flibitijibibo.com/archive/fnalibs.tar.bz2) into the game directory.
* Run `XnaToFna.exe` with Mono on Linux / macOS or .NET Framework on Windows.
* Keep `XnaToFna.exe` in the game directory because the game now also links to the `XnaToFnaHelper` inside it, whoops.
* Update Mono or [MonoKickstart.](https://github.com/flibitijibibo/MonoKickstart)
* Complain about broken paths, unconverted sound / video assets, Windows-specific `DllImport`s and unsupported, closed-source native libraries.


[![how gamers think ports work vs how ports actually work](https://pbs.twimg.com/media/DDVhTJBXYAE11uA.jpg:large)](https://twitter.com/ADAMATOMIC/status/879716288599347200)

#### Special thanks to:

* [Ethan "flibitijibibo" Lee](http://flibitijibibo.com/index.php?page=Portfolio/Ports): Thank you for FNA! This wouldn't have been without you!
* [ADAMATOMIC](https://twitter.com/ADAMATOMIC/status/879716288599347200): for the image above!
* [Iced Lizard Games](http://icedlizardgames.com/) for donating a good bunch of X360 titles and sources, some even unreleased!
* My [Patrons on Patreon](https://www.patreon.com/0x0ade), both current and former ones:
    * [Chad Yates](https://twitter.com/ChadCYates)
    * [KyleTheScientist](https://www.twitch.tv/kylethescientist)
    * razing32
    * Merlijn Sebrechts
	* Ryan Kistner
    * [Renaud Bédard](https://twitter.com/renaudbedard)
    * [Artus Elias Meyer-Toms](https://twitter.com/artuselias)
* razing32 for giving me an UnderRail key to test it with!

#### How?

Using [MonoMod](https://github.com/0x0ade/MonoMod/), it sets up the relinking map in MonoMod to relink all `Microsoft.Xna.Framework` references to `FNA` instead.

It can also relink from `Microsoft.Xna.Framework.Net` and `Microsoft.Xna.Framework.GamerServices` to `FNA.NetStub`, if it exists in the game directory.

Finally, it applies some optional salt and pepper here and there (`XmlIgnore` this, `XnaToFnaHelper.GetProxyFormHandle` that).

#### What's up with XnaToFna-Legacy?

XnaToFna-Legacy was practically a heavily modified fork of MonoMod, meant to relink and only to relink... and fix the paths, which didn't always work.

This rebuilt version *uses* "MonoMod neo", which itself is a rebuilt version of MonoMod. It now supports defining a custom relinker and relinking maps for the default relinker.

Before, most relinker fixes ping-ponged between MonoMod and XnaToFna, while some of them weren't even compatible with each other.

Keeping the actual relinking in MonoMod means there's only one relinker to maintain and only one place where it can screw up.
