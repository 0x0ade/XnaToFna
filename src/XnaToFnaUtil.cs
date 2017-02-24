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
    public partial class XnaToFnaUtil : IDisposable {

        protected static Assembly ThisAssembly = Assembly.GetExecutingAssembly();
        protected static string ThisAssemblyName = ThisAssembly.GetName().Name;

        public List<Tuple<string, string[]>> Mappings = new List<Tuple<string, string[]>> {
            Tuple.Create("FNA", new string[] {
                "Microsoft.Xna.Framework",
                "Microsoft.Xna.Framework.Avatar",
                "Microsoft.Xna.Framework.Content.Pipeline",
                "Microsoft.Xna.Framework.Game",
                "Microsoft.Xna.Framework.Graphics",
                "Microsoft.Xna.Framework.Input.Touch",
                "Microsoft.Xna.Framework.Storage",
                "Microsoft.Xna.Framework.Video",
                "Microsoft.Xna.Framework.Xact"
            }),

            Tuple.Create("MonoGame.Framework.Net", new string[] {
                "Microsoft.Xna.Framework.GamerServices",
                "Microsoft.Xna.Framework.Net"
            }),

            Tuple.Create("FNA.Steamworks", new string[] {
                "FNA.Steamworks",
                "Microsoft.Xna.Framework.GamerServices",
                "Microsoft.Xna.Framework.Net"
            })
        };


        public MonoModder Modder = new MonoModder();

        public DefaultAssemblyResolver AssemblyResolver = new DefaultAssemblyResolver();
        public List<string> Directories = new List<string>();
        public string ContentDirectory;
        public List<ModuleDefinition> Modules = new List<ModuleDefinition>();

        public bool ConvertAudio = true;

        public XnaToFnaUtil() {
            Modder.ReadingMode = ReadingMode.Immediate;

            Modder.AssemblyResolver = AssemblyResolver;
            Modder.DependencyDirs = Directories;

            Modder.Logger = LogMonoMod;
        }
        public XnaToFnaUtil(params string[] paths)
            : this() {

            ScanPaths(paths);
        }

        public void LogMonoMod(string txt) {
            Console.Write("[XnaToFna] [MonoMod] ");
            Console.WriteLine(txt);
        }
        public void Log(string txt) {
            Console.Write("[XnaToFna] ");
            Console.WriteLine(txt);
        }

        public void ScanPaths(params string[] paths) {
            foreach (string path in paths)
                ScanPath(path);
        }

        public void ScanPath(string path) {
            if (Directory.Exists(path)) {
                // Use the directory as "dependency directory" and scan in it.
                if (Directories.Contains(path))
                    // No need to scan the dir if the dir is scanned...
                    return;

                RestoreBackup(path);

                Log($"[ScanPath] Scanning directory {path}");
                Directories.Add(path);
                AssemblyResolver.AddSearchDirectory(path); // Needs to be added manually as DependencyDirs was already added

                if (ContentDirectory == null)
                    if (Directory.Exists(ContentDirectory = Path.Combine(path, "Content")))
                        Log($"[ScanPath] Found Content directory:: {ContentDirectory}");
                    else
                        ContentDirectory = null;

                ScanPaths(Directory.GetFiles(path));
                return;
            }

            if (!path.EndsWith(".dll") && !path.EndsWith(".exe"))
                return;

            // Check if .dll is CLR assembly
            AssemblyName name;
            try {
                name = AssemblyName.GetAssemblyName(path);
            } catch {
                return;
            }

            ReaderParameters modReaderParams = Modder.GenReaderParameters(false);
            // Don't ReadWrite if the module being read is XnaToFna or a relink target.
            modReaderParams.ReadWrite =
                path != ThisAssembly.Location &&
                !Mappings.Exists(mappings => name.Name == mappings.Item1);
            Log($"[ScanPath] Loading assembly {name.Name} ({(modReaderParams.ReadWrite ? "rw" : "r-")})");
            ModuleDefinition mod = ModuleDefinition.ReadModule(path, modReaderParams);
            if ((mod.Attributes & ModuleAttributes.ILOnly) != ModuleAttributes.ILOnly) {
                // Mono.Cecil can't handle mixed mode assemblies.
                return;
            }
            Modules.Add(mod);

            foreach (Tuple<string, string[]> mappings in Mappings)
                if (name.Name == mappings.Item1)
                    foreach (string from in mappings.Item2) {
                        Log($"[ScanPath] Mapping {from} -> {name.Name}");
                        Modder.RelinkModuleMap[from] = mod;
                    }
        }

        public void RestoreBackup(string root) {
            string origRoot = Path.Combine(root, "orig");
            // Check for an "orig" folder to restore any backups from
            if (!Directory.Exists(root))
                return;
            RestoreBackup(root, origRoot);
        }
        public void RestoreBackup(string root, string origRoot) {
            Log($"[RestoreBackup] Restoring from {origRoot} to {root}");
            foreach (string origPath in Directory.EnumerateFiles(origRoot, "*", SearchOption.AllDirectories))
                File.Copy(origPath, root + origPath.Substring(origRoot.Length), true);
        }

        public void OrderModules() {
            List<ModuleDefinition> ordered = new List<ModuleDefinition>(Modules);

            Log("[OrderModules] Unordered: ");
            for (int i = 0; i < Modules.Count; i++)
                Log($"[OrderModules] #{i + 1}: {Modules[i].Assembly.Name.Name}");

            ModuleDefinition dep = null;
            foreach (ModuleDefinition mod in Modules)
                foreach (AssemblyNameReference depName in mod.AssemblyReferences)
                    if (Modules.Exists(other => (dep = other).Assembly.Name.Name == depName.Name)) {
                        Log($"[OrderModules] Reordering {mod.Assembly.Name.Name} dependency {dep.Name}");
                        ordered.Remove(dep);
                        ordered.Insert(ordered.IndexOf(mod), dep);
                    }

            Modules = ordered;

            Log("[OrderModules] Reordered: ");
            for (int i = 0; i < Modules.Count; i++)
                Log($"[OrderModules] #{i + 1}: {Modules[i].Assembly.Name.Name}");
        }

        public void RelinkAll() {
            foreach (ModuleDefinition mod in Modules)
                Modder.DependencyCache[mod.Assembly.Name.Name] = mod;

            foreach (ModuleDefinition mod in Modules)
                Relink(mod);
        }

        public void Relink(ModuleDefinition mod) {
            // Don't relink the relink targets!
            if (Mappings.Exists(mappings => mod.Assembly.Name.Name == mappings.Item1))
                return;

            // Don't relink XnaToFna itself!
            if (mod.Assembly.Name.Name == ThisAssemblyName)
                return;

            Log($"[Relink] Relinking {mod.Assembly.Name.Name}");
            Modder.Module = mod;

            Log($"[Relink] Updating dependencies");
            for (int i = 0; i < mod.AssemblyReferences.Count; i++) {
                AssemblyNameReference dep = mod.AssemblyReferences[i];
                foreach (Tuple<string, string[]> mappings in Mappings)
                    if (mappings.Item2.Contains(dep.Name)) {
                        // Check if module already depends on the remap
                        if (mod.AssemblyReferences.Any(existingDep => existingDep.Name == mappings.Item1)) {
                            // If so, just remove the dependency.
                            mod.AssemblyReferences.RemoveAt(i);
                            i--;
                            break;
                        }
                        // Replace the dependency.
                        mod.AssemblyReferences[i] = new AssemblyNameReference(mappings.Item1, new Version(0, 0, 0, 0));
                        // Only check until first match found.
                        break;
                    }
            }

            Log($"[Relink] Applying MonoMod PatchRefs pass");
            Modder.PatchRefs();

            // Log($"[Relink] Post-processing (XnaToFnaHelper)");
            // TODO XnaToFnaHelper

            Log($"[Relink] Rewriting and disposing module");
            Modder.Module.Write();
            // Dispose the module so other modules can read it as a dependency again.
            Modder.Module.Dispose();
            Modder.Module = null;
            Modder.ClearCaches(moduleSpecific: true);
        }

        public void Dispose() {
            Modder?.Dispose();

            foreach (ModuleDefinition mod in Modules)
                mod.Dispose();
            Modules.Clear();
            Directories.Clear();
        }

    }
}
