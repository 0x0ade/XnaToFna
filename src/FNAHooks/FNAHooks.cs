using Microsoft.Xna.Framework.Content;
using Mono.Cecil;
using MonoMod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using XnaToFna.ContentTransformers;

namespace XnaToFna {
    [FNAHooks]
    public class FNAHooks : Attribute {
    }
}

