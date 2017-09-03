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
    public static class OldModelMesh {

        static OldModelMesh() {
            _OldData<ModelMesh>.Setup = SetupOldData;
            _OldData<ModelMesh>.Disposable.Add("IndexBuffer");
            _OldData<ModelMesh>.Disposable.Add("VertexBuffer");
        }
        internal static void SetupOldData(_OldData<ModelMesh> data, ModelMesh mesh) {
            WeakReference<ModelMesh> weak = data.Weak;
            // FIXME: Finish this!

            data["IndexBuffer"] = null;
            data["VertexBuffer"] = null;
        }

        public static IndexBuffer get_IndexBuffer(this ModelMesh model)
            => model.GetOldData()?.Get<IndexBuffer>("IndexBuffer");

        public static VertexBuffer get_VertexBufferr(this ModelMesh model)
            => model.GetOldData()?.Get<VertexBuffer>("VertexBuffer");

        public static void Draw(this ModelMesh model, SaveStateMode saveStateMode) {
            // TODO: [TimeMachine] Implement SaveStateMode.SaveState
            if (saveStateMode == SaveStateMode.SaveState)
                throw new NotSupportedException("SaveStateMode.SaveState currently not supported by XnaToFna");

            model.Draw();
        }

    }
}
