using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace XnaToFna.ProxyForms {
    public sealed class Application {

        public static event ThreadExceptionEventHandler ThreadException;

        public static string ProductVersion {
            get {
                Assembly asm = Assembly.GetEntryAssembly();
                if (asm == null)
                    return null;

                Module module = asm.ManifestModule;
                if (module != null) {
                    AssemblyInformationalVersionAttribute versionInfo = module.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                    if (versionInfo != null)
                        return versionInfo.InformationalVersion;
                    AssemblyVersionAttribute version = module.GetCustomAttribute<AssemblyVersionAttribute>();
                    if (version != null)
                        return version.Version;
                }

                return asm.GetName().Version.ToString();
            }
        }

        public static string ExecutablePath {
            get {
                return Assembly.GetEntryAssembly().Location;
            }
        }

        public static void Run(Form mainForm) {
            // TODO: Is an Application.Run replacement required?
            try {
                // err...
            } catch (Exception e) {
                // ... what is the sender anyway?
                ThreadException?.Invoke(mainForm, new ThreadExceptionEventArgs(e));
            }
        }

        public static void EnableVisualStyles() {
            // no-op
        }

    }
}
