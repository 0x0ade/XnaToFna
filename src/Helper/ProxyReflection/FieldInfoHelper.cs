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
using System.Globalization;

namespace XnaToFna.ProxyReflection {
    public static class FieldInfoHelper {

        private readonly static Dictionary<Type, Dictionary<string, XnaToFnaFieldInfo>> Map = new Dictionary<Type, Dictionary<string, XnaToFnaFieldInfo>>() {

            { typeof(StringBuilder), new Dictionary<string, XnaToFnaFieldInfo>() {
                { "m_StringValue", new XnaToFnaFieldInfo(
                    typeof(string),
                    (obj) => ((StringBuilder) obj).ToString(),
                    (obj, val) => ((StringBuilder) obj).Clear().Append(val)
                ) },

            } }

        };

        public static FieldInfo GetField(Type self, string name, BindingFlags bindingAttr) {
            Dictionary<string, XnaToFnaFieldInfo> fields;
            XnaToFnaFieldInfo field;
            if (Map.TryGetValue(self, out fields) && fields.TryGetValue(name, out field)) {
                return field;
            }

            return self.GetField(name, bindingAttr);
        }

        public static FieldInfo GetField(Type self, string name) {
            return GetField(self, name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
        }

        public class XnaToFnaFieldInfo : FieldInfo {
            public override FieldAttributes Attributes {
                get {
                    throw new NotSupportedException();
                }
            }

            internal Type _DeclaringType;
            public override Type DeclaringType {
                get {
                    return _DeclaringType;
                }
            }

            public override RuntimeFieldHandle FieldHandle {
                get {
                    throw new NotSupportedException();
                }
            }

            internal readonly Type _FieldType;
            public override Type FieldType {
                get {
                    return _FieldType;
                }
            }

            internal string _Name;
            public override string Name {
                get {
                    return _Name;
                }
            }

            public override Type ReflectedType {
                get {
                    throw new NotSupportedException();
                }
            }

            public XnaToFnaFieldInfo(
                Type fieldType,
                Func<object, object> onGetValue = null,
                Action<object, object> onSetValue = null
            ) {
                _FieldType = fieldType;
                _OnGetValue = onGetValue;
                _OnSetValue = onSetValue;
            }

            public override object[] GetCustomAttributes(bool inherit) {
                throw new NotSupportedException();
            }

            public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
                throw new NotSupportedException();
            }

            public override bool IsDefined(Type attributeType, bool inherit) {
                throw new NotSupportedException();
            }

            internal readonly Func<object, object> _OnGetValue;
            public override object GetValue(object obj)
                => _OnGetValue?.Invoke(obj);

            internal readonly Action<object, object> _OnSetValue;
            public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
                => _OnSetValue?.Invoke(obj, value);
        }

    }
}
