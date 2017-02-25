using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna {
    /// <summary>
    /// Required to initialize the XnaToFnaHelper properly without hardcoding the XnaToFnaHelper.Initialize call.
    /// </summary>
    public class XnaToFnaGame : Game {

        public XnaToFnaGame() {
            XnaToFnaHelper.Initialize(this);
        }
        
    }
}
