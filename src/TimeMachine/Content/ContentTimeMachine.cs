using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.TimeMachine {
    public static partial class ContentTimeMachine {

        public static void Log(string txt) {
            Console.Write("[XnaToFna] [TimeMachine] [Content] ");
            Console.WriteLine(txt);
        }

        public static void UpdateContent(string path, bool patchWaveBanks = true, bool patchXACTSettings = true, bool patchVideo = true) {

            if (patchWaveBanks && path.EndsWith(".xwb")) {
                ContentHelper.PatchContent(path, UpdateWaveBank);
                return;
            }

            if (patchXACTSettings && path.EndsWith(".xgs")) {
                ContentHelper.PatchContent(path, UpdateXACTSettings);
                return;
            }

            if (patchXACTSettings && path.EndsWith(".xsb")) {
                ContentHelper.PatchContent(path, UpdateSoundBank);
                return;
            }

            if (patchXACTSettings && path.EndsWith(".xnb")) {
                ContentHelper.PatchContent(path, UpdateAsset);
                return;
            }

        }

    }
}
