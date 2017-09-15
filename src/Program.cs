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

            bool updateContent = true;

            Queue<string> argq = new Queue<string>(args);
            while (argq.Count > 0) {
                string arg = argq.Dequeue();
                if (arg == "--skip-content")
                    updateContent = false;
                else if (arg == "--skip-wavebanks" || arg == "--skip-xwb")
                    xtf.PatchWaveBanks = false;
                else if (arg == "--skip-xactsettings" || arg == "--skip-xgs")
                    xtf.PatchXACTSettings = false;
                else if (arg == "--skip-video" || arg == "--skip-wma")
                    xtf.PatchVideo = false;
                else if (arg == "--skip-locks" || arg == "--keep-locks")
                    xtf.DestroyLocks = false;
                else if (arg == "--anycpu" || arg == "--force-anycpu")
                    xtf.ForceAnyCPU = true;
                else if (arg == "--keep-mixed-deps") {
                    xtf.StubMixedDeps = false;
                    xtf.DestroyMixedDeps = false;
                } else if (arg == "--stub-mixed-deps") {
                    xtf.StubMixedDeps = true;
                    xtf.DestroyMixedDeps = false;
                } else if (arg == "--remove-mixed-deps") {
                    xtf.StubMixedDeps = false;
                    xtf.DestroyMixedDeps = true;
                } else if (arg == "--fix-old-mono-xml") {
                    Console.WriteLine("YOU SHOULD REALLY UPDATE YOUR COPY OF MONO!... Unless you're stuck with Xamarin.Android.");
                    xtf.FixOldMonoXML = true;
                } else if (arg == "--update-xna" || arg == "--xna3" || arg == "--enable-flux-capacitor") {
                    Console.WriteLine("Please get yourself a copy of XnaToFna from the \"timemachine\" branch to enable the time machine.");
                    return;
                } else if (arg == "--hook-istrialmode") {
                    Console.WriteLine("Do what you want cause a pirate is free! You are a pirate!");
                    xtf.HookIsTrialMode = true;
                } else if (arg == "--content" && argq.Count >= 1) {
                    xtf.ContentDirectoryName = argq.Dequeue();
                } else if (arg.StartsWith("--content=")) {
                    xtf.ContentDirectoryName = arg.Substring("--content=".Length);
                } else
                    xtf.ScanPath(arg);
            }

            if (!Debugger.IsAttached) // Otherwise catches XnaToFna.vshost.exe
                xtf.ScanPath(Directory.GetCurrentDirectory());

            xtf.OrderModules();

            xtf.RelinkAll();

            if (updateContent)
                xtf.UpdateContent();

            xtf.Log("[Main] Done!");

            if (Debugger.IsAttached) // Keep window open when running in IDE
                Console.ReadKey();
        }

    }
}
