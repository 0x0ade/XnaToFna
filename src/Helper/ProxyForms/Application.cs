using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;

namespace XnaToFna.ProxyForms {
    public sealed class Application {

        public static string ProductVersion {
            get {
                return Assembly.GetCallingAssembly().ManifestModule.GetCustomAttribute<AssemblyVersionAttribute>().Version;
            }
        }

        public static string ExecutablePath {
            get {
                return Assembly.GetEntryAssembly().Location;
            }
        }

        public static void Run(Form mainForm) {
            // TODO: Is an Application.Run replacement required?
        }

    }
}
