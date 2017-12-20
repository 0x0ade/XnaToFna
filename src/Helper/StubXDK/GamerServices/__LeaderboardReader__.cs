using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    // Not a XDK type, but some titles refer to signatures mismatching with our replacements.
    public static class __LeaderboardReader__ {

        private static Type t_LeaderboardEntry;
        private static Type t_IList;
        private static Type t_List;
        private static ConstructorInfo ctor_List;
        private static Type t_ReadOnlyCollection;
        private static ConstructorInfo ctor_ReadOnlyCollection;

        [MonoModHook("System.Collections.ObjectModel.ReadOnlyCollection`1<Microsoft.Xna.Framework.GamerServices.LeaderboardEntry> Microsoft.Xna.Framework.GamerServices.LeaderboardReader::get_Entries()")]
        public static object get_Entries(/*LeaderboardReader*/ object reader) {
            // Return an empty collection.
            if (t_LeaderboardEntry == null) {
                t_LeaderboardEntry = StubXDKHelper.GamerServicesAsm.GetType("Microsoft.Xna.Framework.GamerServices.LeaderboardEntry");
            }
            if (t_IList == null) {
                t_IList = typeof(IList<>).MakeGenericType(t_LeaderboardEntry);
            }
            if (t_List == null) {
                t_List = typeof(List<>).MakeGenericType(t_LeaderboardEntry);
                ctor_List = t_List.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { },
                    null
                );
            }
            if (t_ReadOnlyCollection == null) {
                t_ReadOnlyCollection = typeof(ReadOnlyCollection<>).MakeGenericType(t_LeaderboardEntry);
                ctor_ReadOnlyCollection = t_List.GetConstructor(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                    null,
                    new Type[] { t_IList },
                    null
                );
            }
            return ctor_ReadOnlyCollection.Invoke(new object[] { ctor_List.Invoke(new object[] { }) });
        }

        [MonoModHook("System.IAsyncResult Microsoft.Xna.Framework.GamerServices.LeaderboardReader::BeginPageUp(System.AsyncCallback,System.Object)")]
        public static IAsyncResult BeginPageUp(/*LeaderboardReader*/ object reader, AsyncCallback cb, object obj) {
            return null;
        }

        [MonoModHook("System.IAsyncResult Microsoft.Xna.Framework.GamerServices.LeaderboardReader::BeginPageDown(System.AsyncCallback,System.Object)")]
        public static IAsyncResult BeginPageDown(/*LeaderboardReader*/ object reader, AsyncCallback cb, object obj) {
            return null;
        }

        [MonoModHook("System.IAsyncResult Microsoft.Xna.Framework.GamerServices.LeaderboardReader::BeginRead(Microsoft.Xna.Framework.GamerServices.LeaderboardIdentity,System.Int32,System.Int32,System.AsyncCallback,System.Object)")]
        public static IAsyncResult BeginRead(/*LeaderboardIdentity*/ object identity, int a, int b, AsyncCallback cb, object obj) {
            return null;
        }

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.LeaderboardReader::EndPageUp(System.IAsyncResult)")]
        public static void EndPageUp(IAsyncResult result) {

        }

        [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.LeaderboardReader::EndPageDown(System.IAsyncResult)")]
        public static void EndPageDown(IAsyncResult result) {

        }

        [MonoModHook("Microsoft.Xna.Framework.GamerServices.LeaderboardReader Microsoft.Xna.Framework.GamerServices.LeaderboardReader::EndRead(System.IAsyncResult)")]
        public static /*LeaderboardReader*/ object EndRead(IAsyncResult result) {
            return null;
        }

    }
}
