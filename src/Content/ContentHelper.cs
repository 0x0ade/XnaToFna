using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Mono.Cecil;
using MonoMod;
using SDL2;
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
    public static partial class ContentHelper {

        public static bool IsFFMPEGAvailable {
            get {
                try {
                    Process which = new Process();
                    which.StartInfo = new ProcessStartInfo {
                        FileName =
                            (PlatformHelper.Current & Platform.Windows) == Platform.Windows ? "where" :
                            "which",
                        Arguments = "ffmpeg",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    which.Start();
                    which.WaitForExit();
                    return which.ExitCode == 0;
                } catch (Exception e) {
                    Log("Could not determine if FFMPEG available: " + e);
                    return false;
                }
            }
        }

        public static ContentHelperGame Game;

        public static void Log(string txt) {
            Console.Write("[XnaToFna] [ContentHelper] ");
            Console.WriteLine(txt);
        }

        public static void UpdateContent(string path, bool patchXNB = true, bool patchXACT = true, bool patchWindowsMedia = true) {
            if (patchXNB && path.EndsWith(".xnb")) {
                // TransformContent does dirty things; Just use the path.
                TransformContent(path);
                return;
            }

            if (patchXACT && path.EndsWith(".xwb")) {
                PatchContent(path, UpdateWaveBank);
                return;
            }

            if (patchXACT && path.EndsWith(".xsb")) {
                PatchContent(path, UpdateSoundBank);
                return;
            }

            if (patchXACT && path.EndsWith(".xgs")) {
                PatchContent(path, UpdateXACTSettings);
                return;
            }

            if (patchWindowsMedia && path.EndsWith(".wmv")) {
                UpdateVideo(path); // FFMPEG reads from a file and needs to write to another file; Just use the path.
                return;
            }

            if (patchWindowsMedia && path.EndsWith(".wma")) {
                UpdateAudio(path); // FFMPEG reads from a file and needs to write to another file; Just use the path.
                return;
            }

        }

        public static void PatchContent(string path, Action<string, BinaryReader, BinaryWriter> patcher, bool writeToTmp = true, string pathOutput = null) {
            pathOutput = pathOutput ?? path;
            if (writeToTmp)
                File.Delete(path + ".tmp");
            if (pathOutput != path)
                File.Delete(pathOutput);

            using (Stream input = File.OpenRead(path))
            using (BinaryReader reader = new BinaryReader(input))
                if (writeToTmp) {
                    using (Stream output = File.OpenWrite(path + ".tmp"))
                    using (BinaryWriter writer = new BinaryWriter(output))
                        patcher(path, reader, writer);
                } else {
                    patcher(path, reader, null);
                }

            if (writeToTmp) {
                if (pathOutput == path)
                    File.Delete(path);
                File.Move(path + ".tmp", pathOutput);
            }
        }

        public static byte[] SwapEndian(bool swap, byte[] data) {
            if (!swap)
                return data;

            for (int i = data.Length / 2; i > -1; --i) {
                int ii = data.Length - 1 - i;
                byte t = data[i];
                data[i] = data[ii];
                data[ii] = t;
            }

            return data;
        }

        public static ushort SwapEndian(bool swap, ushort data) {
            if (!swap)
                return data;
            return (ushort) (
                ((ushort) ((data & 0xFF) << 8)) |
                ((ushort) ((data >> 8) & 0xFF))
            );
        }

        public static uint SwapEndian(bool swap, uint data) {
            if (!swap)
                return data;
            return
                ((data & 0xFF) << 24) |
                (((data >> 8) & 0xFF) << 16) |
                (((data >> 16) & 0xFF) << 8) |
                ((data >> 24) & 0xFF);
        }

        public static ulong SwapEndian(bool swap, ulong data) {
            if (!swap)
                return data;
            return
                ((data & 0xFF) << 56) |
                (((data >> 8) & 0xFF) << 48) |
                (((data >> 16) & 0xFF) << 40) |
                (((data >> 24) & 0xFF) << 32) |
                (((data >> 32) & 0xFF) << 24) |
                (((data >> 40) & 0xFF) << 16) |
                (((data >> 48) & 0xFF) << 8) |
                ((data >> 56) & 0xFF);
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

            Thread inputPipeThread = input == null ? null : new Thread(
                // Reading from feeder
                feeder != null ? (() => feeder(ffmpeg)) :

                // Reading until the end of input
                inputLength == 0 ? delegate () {
                    input.CopyTo(ffmpeg.StandardInput.BaseStream);
                    ffmpeg.StandardInput.BaseStream.Flush();
                    ffmpeg.StandardInput.BaseStream.Close();
                } :

                // Reading a section from input only
                (ThreadStart) delegate () {
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
                }
            ) {
                IsBackground = true
            };
            inputPipeThread?.Start();

            // Probably writing to file instead.
            if (output == null) {
                ffmpeg.AsyncPipeOut();
                ffmpeg.WaitForExit();
                return;
            }

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
