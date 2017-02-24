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
                else if (arg == "--skip-audio")
                    xtf.ConvertAudio = false;
                else
                    xtf.ScanPath(arg);
            }

            // FIXME detect if ffmpeg exists
            if (xtf.ConvertAudio && false)
                xtf.ConvertAudio = false;

            xtf.ScanPath(Assembly.GetExecutingAssembly().Location);
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
