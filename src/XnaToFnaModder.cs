using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod;
using MonoMod.Utils;
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
    public class XnaToFnaModder : MonoModder {

        public XnaToFnaUtil XTF;
        
        public XnaToFnaModder(XnaToFnaUtil xtf) {
            XTF = xtf;
        }

        public override void Log(string text) {
            // MapDependency clutters the output too much; It's useful for MonoMod itself, but not here.
            if (text.StartsWith("[MapDependency]"))
                return;

            XTF.Log("[MonoMod] " + text);
        }

        public override IMetadataTokenProvider Relinker(IMetadataTokenProvider mtp, IGenericParameterProvider context) {
            // Skip MonoModLinkTo attribute handling.
            return PostRelinker(
                MainRelinker(mtp, context),
                context);
        }

    }
}