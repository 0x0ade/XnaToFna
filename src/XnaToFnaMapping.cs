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

namespace XnaToFna {
    public class XnaToFnaMapping {
        public delegate void SetupDelegate(XnaToFnaUtil xtf, XnaToFnaMapping mapping);

        public bool IsActive;
        public ModuleDefinition Module;

        public string Target;
        public string[] Sources;
        public SetupDelegate Setup;

        public XnaToFnaMapping(string target, string[] sources, SetupDelegate setup = null) {
            Target = target;
            Sources = sources;
            Setup = setup;
        }

    }
}
