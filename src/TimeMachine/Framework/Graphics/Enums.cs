using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace XnaToFna.TimeMachine.Framework.Graphics {

    [RelinkType]
    public enum VertexElementMethod {
        Default = 0,
        UV = 4,
        LookUp = 5,
        LookUpPresampled = 6
    }

}
