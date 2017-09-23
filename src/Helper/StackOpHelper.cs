using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public static class StackOpHelper {

        [ThreadStatic]
        private static Stack<object> Current;

        public readonly static MethodInfo m_Push = typeof(StackOpHelper).GetMethod("Push");
        public static void Push<T>(T value) {
            if (Current == null)
                Current = new Stack<object>();
            Current.Push(value);
        }
        

        public readonly static MethodInfo m_Pop = typeof(StackOpHelper).GetMethod("Pop");
        public static T Pop<T>()
            => (T) Current.Pop();

    }
}
