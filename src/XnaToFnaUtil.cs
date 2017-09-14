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
        public string ContentDirectoryName = "Content";
        public string ContentDirectory;
        public List<ModuleDefinition> Modules = new List<ModuleDefinition>();

        public HashSet<string> RemoveDeps = new HashSet<string>() {
            // Some mixed-mode assemblies refer to nameless dependencies..?
            null,
            "",
            "Microsoft.DirectX.DirectInput",
            "Microsoft.VisualC"

        };
        public List<ModuleDefinition> ModulesToStub = new List<ModuleDefinition>();

        public bool PatchWaveBanks = true;
        public bool PatchXACTSettings = true;
        public bool PatchVideo = true;

        public bool DestroyLocks = true;
        public bool FixOldMonoXML = false;
        public bool DestroyMixedDeps = false;
        public bool StubMixedDeps = true;

        public bool ForceAnyCPU = false;

        public bool HookIsTrialMode = false;

        public XnaToFnaUtil() {
            Modder.ReadingMode = ReadingMode.Immediate;

            Modder.AssemblyResolver = AssemblyResolver;
            Modder.DependencyDirs = Directories;

            Modder.Logger = LogMonoMod;
            Modder.MissingDependencyResolver = MissingDependencyResolver;
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

        public ModuleDefinition MissingDependencyResolver(MonoModder modder, ModuleDefinition main, string name, string fullName) {
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

                if (ContentDirectory == null && Directory.Exists(ContentDirectory = Path.Combine(path, ContentDirectoryName))) {
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
            // Only read debug info if it exists
            if (!File.Exists(path + ".mdb") && !File.Exists(Path.ChangeExtension(path, "pdb")))
                modReaderParams.ReadSymbols = false;
            Log($"[ScanPath] Checking assembly {name.Name} ({(modReaderParams.ReadWrite ? "rw" : "r-")})");
            ModuleDefinition mod = MonoModExt.ReadModule(path, modReaderParams);
            bool add = !modReaderParams.ReadWrite || name.Name == ThisAssemblyName;

            if ((mod.Attributes & ModuleAttributes.ILOnly) != ModuleAttributes.ILOnly) {
                // Mono.Cecil can't handle mixed mode assemblies.
                Log($"[ScanPath] WARNING: Cannot handle mixed mode assembly {name.Name}");
                if (StubMixedDeps) {
                    ModulesToStub.Add(mod);
                    add = true;
                } else {
                    if (DestroyMixedDeps) {
                        RemoveDeps.Add(name.Name);
                    }
                    mod.Dispose();
                    return;
                }
            }

            if (add && !modReaderParams.ReadWrite) { // XNA replacement
                foreach (Tuple<string, string[]> mappings in Mappings)
                    if (name.Name == mappings.Item1)
                        foreach (string from in mappings.Item2) {
                            Log($"[ScanPath] Mapping {from} -> {name.Name}");
                            Modder.RelinkModuleMap[from] = mod;
                        }
            } else if (!add) {
                foreach (Tuple<string, string[]> mappings in Mappings)
                    if (mod.AssemblyReferences.Any(dep => mappings.Item2.Contains(dep.Name))) {
                        add = true;
                        Log($"[ScanPath] XnaToFna-ing {name.Name}");
                        goto BreakMappings;
                    }
            }
            BreakMappings:

            if (add)
                Modules.Add(mod);
            else
                mod.Dispose();

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
                    if (Modules.Exists(other => (dep = other).Assembly.Name.Name == depName.Name) &&
                        ordered.IndexOf(dep) > ordered.IndexOf(mod)) {
                        Log($"[OrderModules] Reordering {mod.Assembly.Name.Name} dependency {dep.Name}");
                        ordered.Remove(mod);
                        ordered.Insert(ordered.IndexOf(dep) + 1, mod);
                    }

            Modules = ordered;

            Log("[OrderModules] Reordered: ");
            for (int i = 0; i < Modules.Count; i++)
                Log($"[OrderModules] #{i + 1}: {Modules[i].Assembly.Name.Name}");
        }

        public void RelinkAll() {
            SetupHelperRelinker();

            foreach (ModuleDefinition mod in Modules)
                Modder.DependencyCache[mod.Assembly.Name.Name] = mod;

            foreach (ModuleDefinition mod in ModulesToStub)
                Stub(mod);

            foreach (ModuleDefinition mod in Modules)
                Relink(mod);
        }

        public void Relink(ModuleDefinition mod) {
            // Don't relink the relink targets!
            if (Mappings.Exists(mappings => mod.Assembly.Name.Name == mappings.Item1))
                return;

            // Don't relink stubbed targets again!
            if (ModulesToStub.Contains(mod))
                return;

            // Don't relink XnaToFna itself!
            if (mod.Assembly.Name.Name == ThisAssemblyName)
                return;

            Log($"[Relink] Relinking {mod.Assembly.Name.Name}");
            Modder.Module = mod;

            Log($"[Relink] Updating dependencies");
            for (int i = 0; i < mod.AssemblyReferences.Count; i++) {
                AssemblyNameReference dep = mod.AssemblyReferences[i];

                // Main mapping mass.
                foreach (Tuple<string, string[]> mappings in Mappings)
                    if (mappings.Item2.Contains(dep.Name) &&
                        // Check if the target module has been found and cached
                        Modder.DependencyCache.ContainsKey(mappings.Item1)) {
                        // Check if module already depends on the remap
                        if (mod.AssemblyReferences.Any(existingDep => existingDep.Name == mappings.Item1)) {
                            // If so, just remove the dependency.
                            mod.AssemblyReferences.RemoveAt(i);
                            i--;
                            goto NextDep;
                        }
                        Log($"[Relink] Replacing dependency {dep.Name} -> {mappings.Item1}");
                        // Replace the dependency.
                        mod.AssemblyReferences[i] = Modder.DependencyCache[mappings.Item1].Assembly.Name;
                        // Only check until first match found.
                        goto NextDep;
                    }

                // Didn't remap; Check for RemoveDeps
                if (RemoveDeps.Contains(dep.Name)) {
                    // Remove any unwanted (f.e. mixed) dependencies.
                    Log($"[Relink] Removing unwanted dependency {dep.Name}");
                    mod.AssemblyReferences.RemoveAt(i);
                    i--;
                    goto NextDep;
                }

                // Didn't remove; Check for ModulesToStub (formerly managed references)
                if (ModulesToStub.Any(stub => stub.Assembly.Name.Name == dep.Name)) {
                    // Fix stubbed dependencies.
                    Log($"[Relink] Fixing stubbed dependency {dep.Name}");
                    // mod.AssemblyReferences.RemoveAt(i);
                    // i--;
                    dep.IsWindowsRuntime = false;
                    dep.HasPublicKey = false;
                    goto NextDep;
                }

                NextDep:
                continue;
            }
            if (!mod.AssemblyReferences.Any(dep => dep.Name == ThisAssemblyName)) {
                // Add XnaToFna as dependency
                Log($"[Relink] Adding dependency XnaToFna");
                mod.AssemblyReferences.Add(Modder.DependencyCache[ThisAssemblyName].Assembly.Name);
            }

            // MonoMod needs to relink some types (f.e. XnaToFnaHelper) via FindType, which requires a dependency map.
            Log("[Relink] Mapping dependencies for MonoMod");
            Modder.MapDependencies(mod);

            if (ModulesToStub.Count != 0) {
                Log("[Relink] Making assembly unsafe");
                mod.Attributes |= ModuleAttributes.ILOnly;
                for (int i = 0; i < mod.Assembly.CustomAttributes.Count; i++) {
                    CustomAttribute attrib = mod.Assembly.CustomAttributes[i];
                    if (attrib.AttributeType.FullName == "System.CLSCompliantAttribute") {
                        mod.Assembly.CustomAttributes.RemoveAt(i);
                        i--;
                    }
                }
                if (!mod.CustomAttributes.Any(ca => ca.AttributeType.FullName == "System.Security.UnverifiableCodeAttribute"))
                    mod.AddAttribute(mod.ImportReference(m_UnverifiableCodeAttribute_ctor));
            }

            Log($"[Relink] Pre-processing");
            mod.Attributes &= ~ModuleAttributes.StrongNameSigned;
            if (ForceAnyCPU)
                mod.Attributes &= ~ModuleAttributes.Required32Bit;
            foreach (TypeDefinition type in mod.Types)
                PreProcessType(type);

            Log($"[Relink] Relinking (MonoMod PatchRefs pass)");
            Modder.PatchRefs();

            Log($"[Relink] Post-processing");
            foreach (TypeDefinition type in mod.Types)
                PostProcessType(type);

            Log($"[Relink] Rewriting and disposing module\n");
            Modder.Module.Write(Modder.WriterParameters);
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
            ModulesToStub.Clear();
            Directories.Clear();
        }

    }
}
