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

        private EventHandler<FindServicesCompletedArgs> _FindServicesCompleted;
        public event EventHandler<FindServicesCompletedArgs> FindServicesCompleted {
            add {
                _FindServicesCompleted += value;
            }
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
