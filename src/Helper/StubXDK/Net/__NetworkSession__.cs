using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.Net {
    // Not a XDK type, but some titles refer to signatures mismatching with our replacements.
    public static class __NetworkSession__ {

        private static Type t_NetworkNotAvailableException;
        private static ConstructorInfo ctor_NetworkNotAvailableException;

        [MonoModHook("System.Int32 Microsoft.Xna.Framework.Net.NetworkSession::get_BytesPerSecondSent()")]
        public static int get_BytesPerSecondSent(/*NetworkSession*/ object session) {
            return 0;
        }

        [MonoModHook("System.Int32 Microsoft.Xna.Framework.Net.NetworkSession::get_BytesPerSecondReceived()")]
        public static int get_BytesPerSecondReceived(/*NetworkSession*/ object session) {
            return 0;
        }

        [MonoModHook("System.IAsyncResult Microsoft.Xna.Framework.Net.NetworkSession::BeginJoinInvited(System.Collections.Generic.IEnumerable`1<Microsoft.Xna.Framework.GamerServices.SignedInGamer>,System.AsyncCallback,System.Object)")]
        public static IAsyncResult BeginJoinInvited(/*IEnumerable<SignedInGamer>*/ object gamers, AsyncCallback cb, object obj) {
            // throw NetworkNotAvailableException
            if (t_NetworkNotAvailableException == null) {
                t_NetworkNotAvailableException = StubXDKHelper.GamerServicesAsm.GetType("Microsoft.Xna.Framework.Net.NetworkNotAvailableException");
                ctor_NetworkNotAvailableException = t_NetworkNotAvailableException.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { },
                    null
                );
            }
            throw (Exception) ctor_NetworkNotAvailableException.Invoke(new object[] { });
        }

        [MonoModHook("System.IAsyncResult Microsoft.Xna.Framework.Net.NetworkSession::BeginJoinInvited(System.Int32,System.AsyncCallback,System.Object)")]
        public static IAsyncResult BeginJoinInvited(int a, AsyncCallback cb, object obj) {
            // throw NetworkNotAvailableException
            if (t_NetworkNotAvailableException == null) {
                t_NetworkNotAvailableException = StubXDKHelper.GamerServicesAsm.GetType("Microsoft.Xna.Framework.Net.NetworkNotAvailableException");
                ctor_NetworkNotAvailableException = t_NetworkNotAvailableException.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { },
                    null
                );
            }
            throw (Exception) ctor_NetworkNotAvailableException.Invoke(new object[] { });
        }

        [MonoModHook("Microsoft.Xna.Framework.Net.NetworkSession Microsoft.Xna.Framework.Net.NetworkSession::EndJoinInvited(System.IAsyncResult)")]
        public static /*NetworkSession*/ object EndJoinInvited(IAsyncResult result) {
            // throw NetworkNotAvailableException
            if (t_NetworkNotAvailableException == null) {
                t_NetworkNotAvailableException = StubXDKHelper.GamerServicesAsm.GetType("Microsoft.Xna.Framework.Net.NetworkNotAvailableException");
                ctor_NetworkNotAvailableException = t_NetworkNotAvailableException.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { },
                    null
                );
            }
            throw (Exception) ctor_NetworkNotAvailableException.Invoke(new object[] { });
        }

    }
}
