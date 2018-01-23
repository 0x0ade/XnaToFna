using Mono.Cecil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Policy;

namespace XnaToFna {
    public static class FNAHooker {

        public static bool Hook(string[] args) {
            if (!AppDomain.CurrentDomain.FriendlyName.EndsWith(" - FNA hooked")) {
                BootHookedAppDomain(args);
                return true;
            }
            BootSucceeded();
            return false;
        }

        public static void BootHookedAppDomain(string[] args) {
            AppDomainSetup nestInfo = new AppDomainSetup();
            nestInfo.ApplicationBase = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            AppDomain nest = AppDomain.CreateDomain(
                AppDomain.CurrentDomain.FriendlyName + " - FNA hooked",
                AppDomain.CurrentDomain.Evidence,
                nestInfo,
                AppDomain.CurrentDomain.PermissionSet
            );
            nest.SetData("FNAHooker.XTFLocation", typeof(XnaToFnaUtil).Assembly.Location);
            string fnaPath = Path.Combine(Path.GetDirectoryName(typeof(XnaToFnaUtil).Assembly.Location), "FNA.dll");
            nest.SetData("FNAHooker.FNALocation", fnaPath);
            string fnaTmpPath = fnaPath + ".tmp";
            nest.SetData("FNAHooker.FNATmpLocation", fnaTmpPath);
            nest.SetData("FNAHooker.Args", args);

            // Prevent the runtime from loading the original FNA.dll
            if (File.Exists(fnaPath)) {
                if (File.Exists(fnaTmpPath))
                    File.Delete(fnaTmpPath);
                File.Move(fnaPath, fnaTmpPath);
            }

            // nest.DoCallBack(Boot);
            ((Proxy) nest.CreateInstanceAndUnwrap(typeof(Proxy).Assembly.FullName, typeof(Proxy).FullName)).Boot();

            AppDomain.Unload(nest);

            // Move FNA.dll back to its original place.
            if (File.Exists(fnaTmpPath)) {
                if (File.Exists(fnaPath))
                    File.Delete(fnaPath);
                File.Move(fnaTmpPath, fnaPath);
            }
        }

        public class Proxy : MarshalByRefObject {
            public void Boot() {
                FNAHooker.Boot();
            }
        }

        public static void Boot() {
            AppDomain domain = AppDomain.CurrentDomain;

            Assembly fna = LoadHookedFNA();
            FNAHookBridgeXTF.Init(fna);

            Assembly xtf = Assembly.LoadFile((string) domain.GetData("FNAHooker.XTFLocation"));
            xtf.EntryPoint.Invoke(null, new object[] { (string[]) domain.GetData("FNAHooker.Args") });
        }

        public static void BootSucceeded() {
            AppDomain domain = AppDomain.CurrentDomain;
            Assembly[] asms = domain.GetAssemblies();
            Assembly fna = null;
            for (int i = 0; i < asms.Length; i++) {
                Assembly asm = asms[i];
                if (asm.GetName().Name == "FNA") {
                    if (fna != null)
                        throw new InvalidProgramException("XnaToFna failed loading _only_ the hooked FNA");
                    fna = asm;
                }
            }
            if (fna == null)
                throw new InvalidProgramException("XnaToFna failed loading _any_ FNA");
        }

        public static Assembly LoadHookedFNA() {
            Console.WriteLine("[XnaToFna] [FNAHooker] Patching FNA in memory...");
            Console.WriteLine("[XnaToFna] [FNAHooker] This is required for the XnaToFna content transformer.");

            AppDomain domain = AppDomain.CurrentDomain;
            Assembly asm;
            using (ModuleDefinition fna = ModuleDefinition.ReadModule((string) domain.GetData("FNAHooker.FNATmpLocation")))
            using (FileStream xtfStream = new FileStream((string) domain.GetData("FNAHooker.XTFLocation"), FileMode.Open, FileAccess.Read))
            using (ModuleDefinition xtfMod = MonoModExt.ReadModule(xtfStream, new ReaderParameters(ReadingMode.Immediate)))
            using (FNAModder fm = new FNAModder() {
                Module = fna,

                // Logger = msg => Console.WriteLine("[XnaToFna] [FNAHooker] " + msg),
                Logger = msg => { },

                CleanupEnabled = false,

                WriterParameters = new WriterParameters() {
                    WriteSymbols = false
                }
            }) {
                fm.DependencyCache[fna.Assembly.Name.FullName] = fna;
                fm.DependencyCache[fna.Assembly.Name.FullName] = fna;

                fm.DependencyCache[xtfMod.Assembly.Name.Name] = xtfMod;
                fm.DependencyCache[xtfMod.Assembly.Name.FullName] = xtfMod;

                fm.ParseRules(xtfMod);
                fm.Mods.Add(xtfMod);
                fm.OnReadMod?.Invoke(xtfMod);

                fm.MapDependencies();

                fm.AutoPatch();

                using (MemoryStream ms = new MemoryStream()) {
                    fm.Write(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    asm = domain.Load(ms.GetBuffer());
                }
            }

            domain.TypeResolve += (object sender, ResolveEventArgs args) => {
                return asm.GetType(args.Name) != null ? asm : null;
            };
            domain.AssemblyResolve += (object sender, ResolveEventArgs args) => {
                return args.Name == asm.FullName || args.Name == asm.GetName().Name ? asm : null;
            };

            return asm;
        }

    }
}

