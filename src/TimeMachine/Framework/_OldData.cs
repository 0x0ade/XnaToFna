using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.CompilerServices;
using XnaToFna.ProxyForms;

namespace XnaToFna.TimeMachine.Framework {
    public class _OldData<TTarget> : IDisposable where TTarget : class {

        // Only use weak references so that T can get freed properly.
        public static ConditionalWeakTable<TTarget, _OldData<TTarget>> ObjectMap = new ConditionalWeakTable<TTarget, _OldData<TTarget>>();
        public static List<string> Disposable = new List<string>();
        public static Action<_OldData<TTarget>, TTarget> Setup = (data, obj) => { };

        public readonly WeakReference<TTarget> Weak;

        private readonly Dictionary<string, object> _VarMap = new Dictionary<string, object>();

        public bool IsAlive {
            get {
                TTarget target;
                return Weak.TryGetTarget(out target);
            }
        }

        public object this[string name] {
            get {
                object value;
                if (!_VarMap.TryGetValue(name, out value))
                    return null;
                return value;
            }
            set {
                object prev;
                if (Disposable.Contains(name) && (prev = this[name]) != null && prev is IDisposable)
                    ((IDisposable) prev).Dispose();
                _VarMap[name] = value;
            }
        }

        public _OldData(TTarget obj) {
            Weak = new WeakReference<TTarget>(obj);
            Setup(this, obj);
        }

        public T Get<T>(string name)
            => (T) this[name];

        public void Set<T>(string name, T value)
            => this[name] = value;

        protected void Dispose(bool disposing) {
            foreach (string name in Disposable) {
                object value;
                if (!_VarMap.TryGetValue(name, out value) || value == null || !(value is IDisposable))
                    continue;
                ((IDisposable) value).Dispose();
            }
            _VarMap.Clear();
        }

        ~_OldData() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }

    public static class _OldDataManager {

        public static _OldData<TTarget> GetOldData<TTarget>(this TTarget obj) where TTarget : class {
            _OldData<TTarget> data;

            if (_OldData<TTarget>.ObjectMap.TryGetValue(obj, out data))
                return data;

            data = new _OldData<TTarget>(obj);
            _OldData<TTarget>.ObjectMap.Add(obj, data);
            return data;
        }

    }

}
