using Mono.Cecil;
using Mono.Options;
using MonoMod;
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

            Console.WriteLine($"XnaToFna {XnaToFnaUtil.Version}");
            Console.WriteLine($"using MonoMod {MonoModder.Version}");

            bool showHelp = false;
            bool showVersion = false;
            bool relinkOnly = false;

            OptionSet options = new OptionSet {
                { "h|help", "Show this message and exit.", v => showHelp = v != null },
                { "v|version", "Show the version and exit.", v => showVersion = v != null },
                { "base=", "Choose between multiple default configs:\ndefault, minimal, forms", v => {
                    switch (v.ToLowerInvariant()) {
                        case "default":
                            xtf.HookCompat = true;
                            xtf.HookHacks = true;
                            xtf.HookEntryPoint = false;
                            xtf.HookLocks = false;
                            xtf.FixOldMonoXML = false;
                            xtf.HookBinaryFormatter = true;
                            xtf.HookReflection = true;
                            break;

                        case "minimal":
                            xtf.HookCompat = false;
                            xtf.HookHacks = false;
                            xtf.HookEntryPoint = false;
                            xtf.HookLocks = false;
                            xtf.FixOldMonoXML = false;
                            xtf.HookBinaryFormatter = false;
                            xtf.HookReflection = false;
                            break;

                        case "forms":
                            xtf.HookCompat = true;
                            xtf.HookHacks = false;
                            xtf.HookEntryPoint = true;
                            xtf.HookLocks = false;
                            xtf.FixOldMonoXML = false;
                            xtf.HookBinaryFormatter = false;
                            xtf.HookReflection = false;
                            break;
                    }
                } },

                { "relinkonly=", "Only read and write the assemblies listed.", (bool v) => relinkOnly = v },

                { "hook-compat=", "Toggle Forms and P/Invoke compatibility hooks.", (bool v) => xtf.HookCompat = v },
                { "hook-hacks=", "Toggle some hack hooks, f.e.\nXNATOFNA_DISPLAY_FULLSCREEN", (bool v) => xtf.HookEntryPoint = v },
                { "hook-locks=", "Toggle if locks should be \"destroyed\" or not.", (bool v) => xtf.HookLocks = v },
                { "hook-oldmonoxml=", "Toggle basic XML serialization fixes.\nPlease try updating mono first!", (bool v) => xtf.FixOldMonoXML = v },
                { "hook-binaryformatter=", "Toggle BinaryFormatter-related fixes.", (bool v) => xtf.HookBinaryFormatter = v },
                { "hook-reflection=", "Toggle reflection-related fixes.", (bool v) => xtf.HookBinaryFormatter = v },
                { "hook-patharg=", "Hook the given method to receive fixed paths.\nCan be used multiple times.", v => xtf.FixPathsFor.Add(v) },

                { "ilplatform=", "Choose the target IL platform:\nkeep, x86, x64, anycpu, x86pref", v => xtf.PreferredPlatform = ParseEnum(v, ILPlatform.Keep) },
                { "mixeddeps=", "Choose the action performed to mixed dependencies:\nkeep, stub, remove", v => xtf.MixedDeps = ParseEnum(v, MixedDepAction.Keep) },
                { "removepublickeytoken=", "Remove the public key token of a dependency.\nCan be used multiple times.", v => xtf.DestroyPublicKeyTokens.Add(v) },
            };

            void WriteHelp(TextWriter writer) {
                writer.WriteLine("Usage: <mono> XnaToFna.exe [options] <--> FileOrDir <FileOrDir> <...>");
                options.WriteOptionDescriptions(writer);
            }

            List<string> extra;
            try {
                extra = options.Parse(args);
            } catch (OptionException e) {
                Console.Error.Write("Command parse error: ");
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine();
                WriteHelp(Console.Error);
                return;
            }

            if (showVersion) {
                return;
            }

            if (showHelp) {
                WriteHelp(Console.Out);
                return;
            }

            foreach (string arg in extra)
                xtf.ScanPath(arg);

            if (!relinkOnly && !Debugger.IsAttached) // Otherwise catches XnaToFna.vshost.exe
                xtf.ScanPath(Directory.GetCurrentDirectory());

            xtf.OrderModules();

            xtf.RelinkAll();

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
