using Mono.Cecil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public static class ContentHelper {

        public static class XWMAInfo {
            public static int[] BytesPerSecond = { 12000, 24000, 4000, 6000, 8000, 20000 };
            public static short[] BlockAlign = { 929, 1487, 1280, 2230, 8917, 8192, 4459, 5945, 2304, 1536, 1485, 1008, 2731, 4096, 6827, 5462 };
        }

        public static void RunFFMPEG(string args, Stream input, Stream output, Action<Process> feeder = null, long inputLength = 0) {
            Process ffmpeg = new Process();
            ffmpeg.StartInfo = new ProcessStartInfo {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };
            ffmpeg.Start();

            ffmpeg.AsyncPipeErr();

            Thread inputPipeThread = new Thread((ThreadStart) (() => feeder(ffmpeg)) ?? delegate () {
                byte[] dataRaw = new byte[4096];
                int sizeRaw;
                Stream ffmpegInStream = ffmpeg.StandardInput.BaseStream;
                long offset = 0;
                while (!ffmpeg.HasExited && offset < inputLength) {
                    offset += sizeRaw = input.Read(dataRaw, 0, Math.Min(dataRaw.Length, (int) (inputLength - offset)));
                    ffmpegInStream.Write(dataRaw, 0, sizeRaw);
                    ffmpegInStream.Flush();
                }
                ffmpegInStream.Close();
            }) {
                IsBackground = true
            };
            inputPipeThread.Start();

            Stream ffmpegStream = ffmpeg.StandardOutput.BaseStream;

            byte[] data = new byte[1024];
            int size;
            while (!ffmpeg.HasExited) {
                size = ffmpegStream.Read(data, 0, data.Length);
                output.Write(data, 0, size);
            }
        }

    }
}
