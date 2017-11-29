using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using MonoMod;
using MonoMod.Detour;
using MonoMod.InlineRT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    // Yo dawg, I heard you like patching...
    public class FNAModder : MonoModder {

        public override void PrePatchType(TypeDefinition type, bool forceAdd = false) {
            // We only patch types marked with FNAHooks.
            if (!HasFNAHooks(type) && !type.HasMMAttribute("__SafeToCopy__"))
                return;
            base.PrePatchType(type, forceAdd);
        }

        public override void PatchType(TypeDefinition type) {
            // We only patch types marked with FNAHooks.
            if (!HasFNAHooks(type) && !type.HasMMAttribute("__SafeToCopy__"))
                return;
            base.PatchType(type);
        }

        public virtual bool HasFNAHooks(TypeDefinition type) {
            return
                type.HasCustomAttribute("XnaToFna.FNAHooks") ||
                (type.DeclaringType == null ? false : HasFNAHooks(type.DeclaringType));
        }

    }
}
