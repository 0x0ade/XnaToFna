using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    public class FindServicesCompletedArgs : EventArgs {

        public ReadOnlyCollection<TitleServiceDescription> Services {
            get {
                return new ReadOnlyCollection<TitleServiceDescription>(new List<TitleServiceDescription>());
            }
        }

    }
}
