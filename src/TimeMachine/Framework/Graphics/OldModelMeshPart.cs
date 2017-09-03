using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using XnaToFna.ProxyForms;

namespace XnaToFna.TimeMachine.Framework.Graphics {
    public static class OldModelMeshPart {

        // Only use weak references so that ModelMeshParts can get disposed properly.
        internal static ConditionalWeakTable<ModelMeshPart, OldModelMeshPartData> Data = new ConditionalWeakTable<ModelMeshPart, OldModelMeshPartData>();
        internal static OldModelMeshPartData GetOldData(this ModelMeshPart part) {
            OldModelMeshPartData data;

            if (Data.TryGetValue(part, out data))
                return data;

            data = new OldModelMeshPartData(part);
            Data.Add(part, data);
            return data;
        }

    }
    
    public class OldModelMeshPartData {
        public OldModelMeshPartData(ModelMeshPart part) {
            WeakReference<ModelMeshPart> weak = new WeakReference<ModelMeshPart>(part);


        }
    }
}
