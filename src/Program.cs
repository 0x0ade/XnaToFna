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

            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
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
                else if (arg == "--keep-mixed-deps") {
                    xtf.StubMixedDeps = false;
                    xtf.DestroyMixedDeps = false;
                } else if (arg == "--stub-mixed-deps") {
                    xtf.StubMixedDeps = true;
                    xtf.DestroyMixedDeps = false;
                } else if (arg == "--remove-mixed-deps") {
                    xtf.StubMixedDeps = false;
                    xtf.DestroyMixedDeps = true;
                } else if (arg == "--update-your-copy-of-mono") {
                    Console.WriteLine("YOU SHOULD REALLY UPDATE YOUR COPY OF MONO!... Unless you're stuck with Xamarin.Android.");
                    xtf.FixOldMonoXML = true;
                } else if (arg == "--update-xna" || arg == "--xna3" || arg == "--enable-flux-capacitor") {
                    Console.WriteLine("\"When this baby hits 88 mph, we're gonna see some serious shit.\"");
                    Console.WriteLine("    -- Doc Brown, 1995");
                    xtf.EnableTimeMachine = true;
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
