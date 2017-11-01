using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna {
    public class Program {

        public static void Main(string[] args) {
            XnaToFnaUtil xtf = new XnaToFnaUtil();

            xtf.Log($"[Version] {MonoMod.MonoModder.Version}");

            bool updateContent = true;

            Queue<string> argq = new Queue<string>(args);
            while (argq.Count > 0) {
                string arg = argq.Dequeue();
                if (arg == "--version" || arg.ToLowerInvariant() == "-v")
                    return;

                else if (arg == "--mm-strict")
                    xtf.Modder.Strict = true;

                else if (arg == "--skip-entrypoint") {
                    Console.WriteLine("Skipping entry point hook. This will limit and even disable some runtime features.");
                    xtf.HookEntryPoint = false;
                }

                else if (arg == "--skip-content")
                    updateContent = false;
                else if (arg == "--skip-xnb")
                    xtf.PatchXNB = false;
                else if (arg == "--skip-xact")
                    xtf.PatchXACT = false;
                else if (arg == "--skip-windowsmedia" || arg == "--skip-wm")
                    xtf.PatchWindowsMedia = false;
                else if (
                    arg == "--skip-wavebanks" || arg == "--skip-xwb" ||
                    arg == "--skip-soundbanks" || arg == "--skip-xsb" ||
                    arg == "--skip-xactsettings" || arg == "--skip-xgs")
                    Console.WriteLine("WARNING: --skip-xwb, --skip-xsb and --skip-xsg have been replaced with --skip-xact.");
                else if (arg == "--skip-video" || arg == "--skip-wma")
                    Console.WriteLine("WARNING: --skip-video and --skip-wma have been replaced with --skip-wm.");

                else if (arg == "--skip-locks" || arg == "--keep-locks")
                    xtf.DestroyLocks = false;

                else if (arg == "--decompress-xnb" || arg == "--skip-gzip")
                    ContentHelper.XNBCompressGZip = false;

                else if (arg == "--anycpu" || arg == "--force-anycpu")
                    Console.WriteLine("WARNING: --anycpu / --force-anycpu is now default. To set the preferred platform, use --platform x86 / x64 instead.");
                else if (arg == "--platform" && argq.Count >= 1)
                    xtf.PreferredPlatform = ParseEnum(argq.Dequeue(), ILPlatform.AnyCPU);
                else if (arg.StartsWith("--platform="))
                    xtf.PreferredPlatform = ParseEnum(arg.Substring("--platform=".Length), ILPlatform.AnyCPU);

                else if (arg == "--keep-mixed-deps") {
                    xtf.StubMixedDeps = false;
                    xtf.DestroyMixedDeps = false;
                } else if (arg == "--stub-mixed-deps") {
                    xtf.StubMixedDeps = true;
                    xtf.DestroyMixedDeps = false;
                } else if (arg == "--remove-mixed-deps") {
                    xtf.StubMixedDeps = false;
                    xtf.DestroyMixedDeps = true;
                } else if (arg == "--remove-public-key-token" && argq.Count >= 1)
                    xtf.DestroyPublicKeyTokens.Add(argq.Dequeue());
                else if (arg.StartsWith("--remove-public-key-token="))
                    xtf.DestroyPublicKeyTokens.Add(arg.Substring("--remove-public-key-token=".Length));

                else if (arg == "--fix-old-mono-xml") {
                    Console.WriteLine("YOU SHOULD REALLY UPDATE YOUR COPY OF MONO!... Unless you're stuck with Xamarin.Android.");
                    xtf.FixOldMonoXML = true;

                } else if (arg == "--update-xna" || arg == "--xna3" || arg == "--enable-flux-capacitor") {
                    Console.WriteLine("Please get yourself a copy of XnaToFna from the \"timemachine\" branch to enable the time machine.");
                    return;
                } else if (arg == "--hook-istrialmode") {
                    Console.WriteLine("Do what you want cause a pirate is free! You are a pirate!");
                    xtf.HookIsTrialMode = true;
                }

                else if (arg == "--content" && argq.Count >= 1)
                    xtf.ContentDirectoryName = argq.Dequeue();
                else if (arg.StartsWith("--content="))
                    xtf.ContentDirectoryName = arg.Substring("--content=".Length);

                else if (arg == "--skip-binaryformatter" || arg == "--skip-bf")
                    xtf.HookBinaryFormatter = false;

                else if (arg == "--fix-path-arg" && argq.Count >= 1)
                    xtf.FixPathsFor.Add(argq.Dequeue());
                else if (arg.StartsWith("--fix-path-arg="))
                    xtf.FixPathsFor.Add(arg.Substring("--fix-path-arg=".Length));

                else
                    xtf.ScanPath(arg);
            }

            if (!Debugger.IsAttached) // Otherwise catches XnaToFna.vshost.exe
                xtf.ScanPath(Directory.GetCurrentDirectory());

            xtf.OrderModules();

            xtf.RelinkAll();

            if (updateContent) {
                xtf.LoadModules();
                xtf.UpdateContent();
            }

            xtf.Log("[Main] Done!");

            if (Debugger.IsAttached) // Keep window open when running in IDE
                Console.ReadKey();
        }

        private static T ParseEnum<T>(string value, T defaultResult) where T : struct {
            T result;
            if (Enum.TryParse<T>(value, true, out result))
                return result;
            return defaultResult;
        }

    }
}
