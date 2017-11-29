using Mono.Cecil;
using Mono.Cecil.Cil;
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
using XnaToFna.XEX;

namespace XnaToFna {
    public partial class XnaToFnaUtil : IDisposable {

        public readonly static byte[] DotNetFrameworkKeyToken = { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 }; // b77a5c561934e089
        public readonly static Version DotNetFramework4Version = new Version(4, 0, 0, 0);
        public readonly static Version DotNetFramework2Version = new Version(2, 0, 0, 0);

        public readonly static Version DotNetX360Version = new Version(2, 0, 5, 0);

        public readonly static Assembly ThisAssembly = Assembly.GetExecutingAssembly();
        public readonly static string ThisAssemblyName = ThisAssembly.GetName().Name;
        public readonly static Version Version = ThisAssembly.GetName().Version;

        public readonly ModuleDefinition ThisModule;

        public List<XnaToFnaMapping> Mappings = new List<XnaToFnaMapping> {
            new XnaToFnaMapping("FNA", new string[] {
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

            new XnaToFnaMapping("MonoGame.Framework.Net", new string[] {
                "Microsoft.Xna.Framework.GamerServices",
                "Microsoft.Xna.Framework.Net"
            }, SetupDirectRelinkMap),

            new XnaToFnaMapping("FNA.Steamworks", new string[] {
                "FNA.Steamworks",
                "Microsoft.Xna.Framework.GamerServices",
                "Microsoft.Xna.Framework.Net"
            }, SetupDirectRelinkMap)
        };


        public MonoModder Modder = new MonoModder();

        public DefaultAssemblyResolver AssemblyResolver = new DefaultAssemblyResolver();
        public List<string> Directories = new List<string>();
        public List<string> ContentDirectoryNames = new List<string>() { "Content" };
        public List<string> ContentDirectories = new List<string>();
        public List<ModuleDefinition> Modules = new List<ModuleDefinition>();
        public Dictionary<ModuleDefinition, string> ModulePaths = new Dictionary<ModuleDefinition, string>();

        public HashSet<string> RemoveDeps = new HashSet<string>() {
            // Some mixed-mode assemblies refer to nameless dependencies..?
            null,
            "",
            "Microsoft.DirectX.DirectInput",
            "Microsoft.VisualC"

        };
        public List<ModuleDefinition> ModulesToStub = new List<ModuleDefinition>();

        public List<string> ExtractedXEX = new List<string>();

        public bool HookEntryPoint = true;

        public bool PatchXNB = true;
        public bool PatchXACT = true;
        public bool PatchWindowsMedia = true;

        public bool DestroyLocks = true;
        public bool FixOldMonoXML = false;
        public bool DestroyMixedDeps = false;
        public bool StubMixedDeps = true;
        public bool HookIsTrialMode = false;
        public bool HookBinaryFormatter = true;

        public List<string> DestroyPublicKeyTokens = new List<string>();

        public List<string> FixPathsFor = new List<string>();

        public ILPlatform PreferredPlatform = ILPlatform.AnyCPU;

        public XnaToFnaUtil() {
            Modder.ReadingMode = ReadingMode.Immediate;

            Modder.Strict = false;

            Modder.AssemblyResolver = AssemblyResolver;
            Modder.DependencyDirs = Directories;

            Modder.Logger = LogMonoMod;
            Modder.MissingDependencyResolver = MissingDependencyResolver;

            using (FileStream xtfStream = new FileStream(Assembly.GetExecutingAssembly().Location, FileMode.Open, FileAccess.Read))
                ThisModule = MonoModExt.ReadModule(xtfStream, new ReaderParameters(ReadingMode.Immediate));
            Modder.DependencyCache[ThisModule.Assembly.Name.Name] = ThisModule;
            Modder.DependencyCache[ThisModule.Assembly.Name.FullName] = ThisModule;
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

                foreach (string contentDirName in ContentDirectoryNames) {
                    string contentDir;
                    if (Directory.Exists(contentDir = Path.Combine(path, contentDirName))) {
                        if (ContentDirectories.Count == 0) {
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
                                else if (File.Exists(Path.Combine(Path.GetDirectoryName(ThisAssembly.Location), "FNA.dll.tmp")))
                                    File.Copy(Path.Combine(Path.GetDirectoryName(ThisAssembly.Location), "FNA.dll.tmp"), Path.Combine(path, "FNA.dll"), true);

                            }
                        }
                        Log($"[ScanPath] Found Content directory: {contentDir}");
                        ContentDirectories.Add(contentDir);
                    }
                }

                ScanPaths(Directory.GetFiles(path));
                return;
            }

            if (File.Exists(path + ".xex")) {
                if (!ExtractedXEX.Contains(path)) {
                    // Remove the original file - let XnaToFna unpack and handle it later.
                    File.Delete(path);
                } else {
                    // XnaToFna will handle the .xex instead.
                }
                return;
            }

            if (path.EndsWith(".xex")) {
                string pathTarget = path.Substring(0, path.Length - 4);
                if (string.IsNullOrEmpty(Path.GetExtension(pathTarget)))
                    return;

                using (Stream streamXEX = File.OpenRead(path))
                using (BinaryReader reader = new BinaryReader(streamXEX))
                using (Stream streamRAW = File.OpenWrite(pathTarget)) {
                    XEXImageData data = new XEXImageData(reader);

                    int offset = 0;
                    int size = data.m_memorySize;

                    // Check if this file is a PE containing an embedded PE.
                    if (data.m_memorySize > 0x10000) { // One default segment alignment.
                        using (MemoryStream streamMEM = new MemoryStream(data.m_memoryData))
                        using (BinaryReader mem = new BinaryReader(streamMEM)) {
                            if (mem.ReadUInt32() != 0x00905A4D) // MZ
                                goto WriteRaw;
                            // This is horrible.
                            streamMEM.Seek(0x00000280, SeekOrigin.Begin);
                            if (mem.ReadUInt64() != 0x000061746164692E) // ".idata\0\0"
                                goto WriteRaw;
                            streamMEM.Seek(0x00000288, SeekOrigin.Begin);
                            mem.ReadInt32(); // Virtual size; It's somewhat incorrect?
                            offset = mem.ReadInt32(); // Virtual offset.
                            // mem.ReadInt32(); // Raw size; Still incorrect.
                            // Let's just write everything...
                            size = data.m_memorySize - offset;
                        }
                    }

                    WriteRaw:
                    streamRAW.Write(data.m_memoryData, offset, size);
                }

                path = pathTarget;
                ExtractedXEX.Add(pathTarget);
            } else if (!path.EndsWith(".dll") && !path.EndsWith(".exe"))
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
                !Mappings.Exists(mappings => name.Name == mappings.Target);
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
                foreach (XnaToFnaMapping mapping in Mappings)
                    if (name.Name == mapping.Target) {
                        mapping.IsActive = true;
                        mapping.Module = mod;
                        foreach (string from in mapping.Sources) {
                            Log($"[ScanPath] Mapping {from} -> {name.Name}");
                            Modder.RelinkModuleMap[from] = mod;
                        }
                    }
            } else if (!add) {
                foreach (XnaToFnaMapping mapping in Mappings)
                    if (mod.AssemblyReferences.Any(dep => mapping.Sources.Contains(dep.Name))) {
                        add = true;
                        Log($"[ScanPath] XnaToFna-ing {name.Name}");
                        goto BreakMappings;
                    }
            }
            BreakMappings:

            if (add) {
                Modules.Add(mod);
                ModulePaths[mod] = path;
            } else
                mod.Dispose();

        }

        public void RestoreBackup(string root) {
            string origRoot = Path.Combine(root, "orig");
            // Check for an "orig" folder to restore any backups from
            if (!Directory.Exists(origRoot))
                return;
            RestoreBackup(root, origRoot);
        }
        public void RestoreBackup(string root, string origRoot) {
            Log($"[RestoreBackup] Restoring from {origRoot} to {root}");
            foreach (string origPath in Directory.EnumerateFiles(origRoot, "*", SearchOption.AllDirectories)) {
                Directory.CreateDirectory(Path.GetDirectoryName(root + origPath.Substring(origRoot.Length)));
                File.Copy(origPath, root + origPath.Substring(origRoot.Length), true);
            }
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
            if (Mappings.Exists(mappings => mod.Assembly.Name.Name == mappings.Target))
                return;

            // Don't relink stubbed targets again!
            if (ModulesToStub.Contains(mod))
                return;

            // Don't relink XnaToFna itself!
            if (mod.Assembly.Name.Name == ThisAssemblyName)
                return;

            Log($"[Relink] Relinking {mod.Assembly.Name.Name}");
            Modder.Module = mod;

            ApplyCommonChanges(mod);

            Log("[Relink] Pre-processing");
            foreach (TypeDefinition type in mod.Types)
                PreProcessType(type);

            Log("[Relink] Relinking (MonoMod PatchRefs pass)");
            Modder.PatchRefs();

            Log("[Relink] Post-processing");
            foreach (TypeDefinition type in mod.Types)
                PostProcessType(type);

            if (HookEntryPoint && mod.EntryPoint != null) {
                Log("[Relink] Injecting XnaToFna entry point hook");
                ILProcessor il = mod.EntryPoint.Body.GetILProcessor();
                Instruction call = il.Create(OpCodes.Call, mod.ImportReference(m_XnaToFnaHelper_MainHook));
                il.InsertBefore(mod.EntryPoint.Body.Instructions[0], call);
                il.InsertBefore(call, il.Create(OpCodes.Ldarg_0));
            }

            Log("[Relink] Rewriting and disposing module\n");
            Modder.Module.Write(Modder.WriterParameters);
            // Dispose the module so other modules can read it as a dependency again.
            Modder.Module.Dispose();
            Modder.Module = null;
            Modder.ClearCaches(moduleSpecific: true);
        }

        public void ApplyCommonChanges(ModuleDefinition mod, string tag = "Relink") {
            if (DestroyPublicKeyTokens.Contains(mod.Assembly.Name.Name)) {
                Log($"[{tag}] Destroying public key token for module {mod.Assembly.Name.Name}");
                mod.Assembly.Name.PublicKeyToken = new byte[0];
            }

            Log($"[{tag}] Updating dependencies");
            for (int i = 0; i < mod.AssemblyReferences.Count; i++) {
                AssemblyNameReference dep = mod.AssemblyReferences[i];

                // Main mapping mass.
                foreach (XnaToFnaMapping mapping in Mappings)
                    if (mapping.Sources.Contains(dep.Name) &&
                        // Check if the target module has been found and cached
                        Modder.DependencyCache.ContainsKey(mapping.Target)) {
                        // Check if module already depends on the remap
                        if (mod.AssemblyReferences.Any(existingDep => existingDep.Name == mapping.Target)) {
                            // If so, just remove the dependency.
                            mod.AssemblyReferences.RemoveAt(i);
                            i--;
                            goto NextDep;
                        }
                        Log($"[{tag}] Replacing dependency {dep.Name} -> {mapping.Target}");
                        // Replace the dependency.
                        mod.AssemblyReferences[i] = Modder.DependencyCache[mapping.Target].Assembly.Name;
                        // Only check until first match found.
                        goto NextDep;
                    }

                // Didn't remap; Check for RemoveDeps
                if (RemoveDeps.Contains(dep.Name)) {
                    // Remove any unwanted (f.e. mixed) dependencies.
                    Log($"[{tag}] Removing unwanted dependency {dep.Name}");
                    mod.AssemblyReferences.RemoveAt(i);
                    i--;
                    goto NextDep;
                }

                // Didn't remove

                // Check for DestroyPublicKeyTokens
                if (DestroyPublicKeyTokens.Contains(dep.Name)) {
                    Log($"[{tag}] Destroying public key token for dependency {dep.Name}");
                    dep.PublicKeyToken = new byte[0];
                }

                // Check for ModulesToStub (formerly managed references)
                if (ModulesToStub.Any(stub => stub.Assembly.Name.Name == dep.Name)) {
                    // Fix stubbed dependencies.
                    Log($"[{tag}] Fixing stubbed dependency {dep.Name}");
                    dep.IsWindowsRuntime = false;
                    dep.HasPublicKey = false;
                }

                // Check for .NET compact (X360) version
                if (dep.Version == DotNetX360Version) {
                    // Replace public key token.
                    dep.PublicKeyToken = DotNetFrameworkKeyToken;
                    // Technically .NET 2(?), but let's just bump the version.
                    dep.Version = DotNetFramework4Version;
                }

                NextDep:
                continue;
            }
            if (!mod.AssemblyReferences.Any(dep => dep.Name == ThisAssemblyName)) {
                // Add XnaToFna as dependency
                Log($"[{tag}] Adding dependency XnaToFna");
                mod.AssemblyReferences.Add(Modder.DependencyCache[ThisAssemblyName].Assembly.Name);
            }

            if (mod.Runtime < TargetRuntime.Net_4_0) {
                // XNA 3.0 / 3.1 and X360 games depend on a .NET Framework pre-4.0
                mod.Runtime = TargetRuntime.Net_4_0;
                // TODO: What about the System.*.dll dependencies?
            }

            Log($"[{tag}] Updating module attributes");
            mod.Attributes &= ~ModuleAttributes.StrongNameSigned;
            if (PreferredPlatform != ILPlatform.Keep) {
                // "Clear" to AnyCPU.
                mod.Architecture = TargetArchitecture.I386;
                mod.Attributes &= ~ModuleAttributes.Required32Bit & ~ModuleAttributes.Preferred32Bit;

                switch (PreferredPlatform) {
                    case ILPlatform.x86:
                        mod.Architecture = TargetArchitecture.I386;
                        mod.Attributes |= ModuleAttributes.Required32Bit;
                        break;
                    case ILPlatform.x64:
                        mod.Architecture = TargetArchitecture.AMD64;
                        break;
                    case ILPlatform.x86Pref:
                        mod.Architecture = TargetArchitecture.I386;
                        mod.Attributes |= ModuleAttributes.Preferred32Bit;
                        break;
                }
            }

            bool mixed = (mod.Attributes & ModuleAttributes.ILOnly) != ModuleAttributes.ILOnly;
            if (ModulesToStub.Count != 0 || mixed) {
                Log($"[{tag}] Making assembly unsafe");
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

            // MonoMod needs to relink some types (f.e. XnaToFnaHelper) via FindType, which requires a dependency map.
            Log($"[{tag}] Mapping dependencies for MonoMod");
            Modder.MapDependencies(mod);
        }

        public void LoadModules() {
            // Load all modules, except map targets and stubs.
            foreach (ModuleDefinition mod in Modules) {
                if (Mappings.Exists(mappings => mod.Assembly.Name.Name == mappings.Target) ||
                    mod.Assembly.Name.Name == ThisAssemblyName ||
                    ModulesToStub.Contains(mod))
                    continue;

                Assembly asm = Assembly.LoadFile(ModulePaths[mod]);
                AppDomain.CurrentDomain.TypeResolve += (object sender, ResolveEventArgs args) => {
                    return asm.GetType(args.Name) != null ? asm : null;
                };
                AppDomain.CurrentDomain.AssemblyResolve += (object sender, ResolveEventArgs args) => {
                    return args.Name == asm.FullName || args.Name == asm.GetName().Name ? asm : null;
                };
            }
        }

        public void UpdateContent() {
            if (ContentDirectories.Count == 0) {
                Log("[UpdateContent] No content directory found!");
                return;
            }

            // Since we're patching XNBs, we need to do this inside a game.
            using (ContentHelper.Game = new ContentHelperGame()) {
                ContentHelper.Game.ActionQueue.Enqueue(() => {
                    // List all content files and update accordingly.
                    foreach (string dir in ContentDirectories)
                        foreach (string path in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
                            ContentHelper.UpdateContent(path, PatchXNB, PatchXACT, PatchWindowsMedia);
                });
                ContentHelper.Game.Run();
            }
            ContentHelper.Game = null;
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
