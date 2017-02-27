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

        public bool PatchWaveBanks = true;
        public bool PatchXACTSettings = true;
        public bool PatchVideo = true;

        public bool DestroyLocks = true;

        public XnaToFnaUtil() {
            Modder.ReadingMode = ReadingMode.Immediate;

            Modder.AssemblyResolver = AssemblyResolver;
            Modder.DependencyDirs = Directories;

            Modder.Logger = LogMonoMod;
            Modder.MissingDependencyResolver = MissingDependencyResolver;

            SetupHelperRelinkMap();
        }
        public XnaToFnaUtil(params string[] paths)
            : this() {

            ScanPaths(paths);
        }

        public void LogMonoMod(string txt) {
            // MapDependency clutters the output too much; It's useful for MonoMod itself, but not here.
            if (txt.StartsWith("[MapDependency]"))
                return;

            Console.Write("[XnaToFna] [MonoMod] ");
            Console.WriteLine(txt);
        }
        public void Log(string txt) {
            Console.Write("[XnaToFna] ");
            Console.WriteLine(txt);
        }

        public ModuleDefinition MissingDependencyResolver(ModuleDefinition main, string name, string fullName) {
            LogMonoMod($"Cannot map dependency {main.Name} -> (({fullName}), ({name})) - not found");
            return null;
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

                if (ContentDirectory == null && Directory.Exists(ContentDirectory = Path.Combine(path, "Content"))) {
                    // Most probably the actual game directory - let's just copy XnaToFna.exe to there to be referenced properly.
                    string xtfPath = Path.Combine(path, Path.GetFileName(ThisAssembly.Location));
                    if (Path.GetDirectoryName(ThisAssembly.Location) != path) {
                        Log($"[ScanPath] Found separate game directory - copying XnaToFna.exe and FNA.dll");
                        File.Copy(ThisAssembly.Location, xtfPath, true);

                        string dbExt = null;
                        if (File.Exists(Path.ChangeExtension(ThisAssembly.Location, "pdb")))
                            dbExt = "pdb";
                        if (File.Exists(Path.ChangeExtension(ThisAssembly.Location, "mdb")))
                            dbExt = "mdb";
                        if (dbExt != null)
                            File.Copy(Path.ChangeExtension(ThisAssembly.Location, dbExt), Path.ChangeExtension(xtfPath, dbExt), true);

                        if (File.Exists(Path.Combine(Path.GetDirectoryName(ThisAssembly.Location), "FNA.dll")))
                            File.Copy(Path.Combine(Path.GetDirectoryName(ThisAssembly.Location), "FNA.dll"), Path.Combine(path, "FNA.dll"), true);

                    }
                    Log($"[ScanPath] Found Content directory: {ContentDirectory}");
                } else {
                    ContentDirectory = null;
                }

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
            // TODO Dispose those?
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
                    if (mappings.Item2.Contains(dep.Name) &&
                        // Check if the target module has been found and cached
                        Modder.DependencyCache.ContainsKey(mappings.Item1)) {
                        // Check if module already depends on the remap
                        if (mod.AssemblyReferences.Any(existingDep => existingDep.Name == mappings.Item1)) {
                            // If so, just remove the dependency.
                            mod.AssemblyReferences.RemoveAt(i);
                            i--;
                            break;
                        }
                        // Replace the dependency.
                        mod.AssemblyReferences[i] = Modder.DependencyCache[mappings.Item1].Assembly.Name;
                        // Only check until first match found.
                        break;
                    }
            }
            if (!mod.AssemblyReferences.Any(dep => dep.Name == ThisAssemblyName)) {
                // Add XnaToFna as dependency
                mod.AssemblyReferences.Add(Modder.DependencyCache[ThisAssemblyName].Assembly.Name);
            }

            // MonoMod needs to relink some types (f.e. XnaToFnaHelper) via FindType, which requires a dependency map.
            Log("[Relink] Mapping dependencies for MonoMod");
            Modder.MapDependencies(mod);

            Log($"[Relink] Pre-processing");
            foreach (TypeDefinition type in mod.Types)
                PreProcessType(type);

            Log($"[Relink] Relinking (MonoMod PatchRefs pass)");
            Modder.PatchRefs();

            Log($"[Relink] Post-processing");
            foreach (TypeDefinition type in mod.Types)
                PostProcessType(type);

            Log($"[Relink] Rewriting and disposing module\n");
            Modder.Module.Write();
            // Dispose the module so other modules can read it as a dependency again.
            Modder.Module.Dispose();
            Modder.Module = null;
            Modder.ClearCaches(moduleSpecific: true);
        }

        public void UpdateContent() {
            // Verify ContentDirectory path
            if (ContentDirectory != null && !Directory.Exists(ContentDirectory))
                ContentDirectory = null;

            if (ContentDirectory == null) {
                Log("[UpdateContent] No content directory found!");
                return;
            }

            // List all content files and update accordingly.
            foreach (string path in Directory.EnumerateFiles(ContentDirectory, "*", SearchOption.AllDirectories))
                ContentHelper.UpdateContent(path, PatchWaveBanks, PatchXACTSettings, PatchVideo);
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
