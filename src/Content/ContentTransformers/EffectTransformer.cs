using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using Mono.Cecil;
using MonoMod;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna.ContentTransformers {
    public class EffectTransformer : ContentTypeReader<Effect> {

        protected override Effect Read(ContentReader input, Effect existing) {
            // TODO: How are we going to handle X360 effects?!
            input.ReadBytes(input.ReadInt32());
            return existing;
        }

    }
}
