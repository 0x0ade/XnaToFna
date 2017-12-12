using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    public class TitleServiceDirectory {

        private event EventHandler<FindServicesCompletedArgs> _FindServicesCompleted;
        public event EventHandler<FindServicesCompletedArgs> FindServicesCompleted {
            [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.TitleServiceDirectory::add_FindServicesCompleted(System.EventHandler`1<Microsoft.Xna.Framework.GamerServices.FindServicesCompletedArgs>)")]
            add {
                _FindServicesCompleted += value;
            }
            [MonoModHook("System.Void Microsoft.Xna.Framework.GamerServices.TitleServiceDirectory::remove_FindServicesCompleted(System.EventHandler`1<Microsoft.Xna.Framework.GamerServices.FindServicesCompletedArgs>)")]
            remove {
                _FindServicesCompleted -= value;
            }
        }

        public bool IsBusy {
            get {
                return false;
            }
        }

        public TitleServiceDirectory() {

        }

        public void FindServicesAsync() {

        }

    }
}
