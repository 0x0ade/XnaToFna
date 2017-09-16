using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public static class BinaryFormatterHelper {
        public readonly static Assembly FNA = typeof(Game).Assembly;

        public static BinaryFormatter Create() {
            BinaryFormatter bf = new BinaryFormatter();
            bf.Binder = new XnaToFnaSerializationBinderWrapper(null);
            return bf;
        }

        public static BinaryFormatter Create(ISurrogateSelector selector, StreamingContext context) {
            BinaryFormatter bf = new BinaryFormatter(selector, context);
            bf.Binder = new XnaToFnaSerializationBinderWrapper(null);
            return bf;
        }

        public static SerializationBinder get_Binder(this BinaryFormatter self)
            => (self.Binder as XnaToFnaSerializationBinderWrapper)?.Inner ?? self.Binder;
        public static void set_Binder(this BinaryFormatter self, SerializationBinder binder)
            => self.Binder = new XnaToFnaSerializationBinderWrapper(binder);

        public class XnaToFnaSerializationBinderWrapper : SerializationBinder {
            public readonly SerializationBinder Inner;

            public XnaToFnaSerializationBinderWrapper(SerializationBinder inner) {
                Inner = inner;
            }

            public override Type BindToType(string assemblyName, string typeName) {
                if (assemblyName != "Microsoft.Xna.Framework" && !assemblyName.StartsWith("Microsoft.Xna.Framework."))
                    return Inner?.BindToType(assemblyName, typeName);
                return FNA.GetType(typeName);
            }
        }
    }
}
