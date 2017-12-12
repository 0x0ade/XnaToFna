using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK {
    public static class StubXDKHelper {

        private static Assembly _GamerServicesAsm;
        public static Assembly GamerServicesAsm {
            get {
                if (_GamerServicesAsm != null) {
                    return _GamerServicesAsm;
                }

                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in asms) {
                    if (asm.GetType("Microsoft.Xna.Framework.GamerServices.GamerPresence") != null) {
                        return _GamerServicesAsm = asm;
                    }
                }

                return null;
            }
        }

    }
}
