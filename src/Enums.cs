using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna {
    public enum ILPlatform {
        Keep,
        x86,
        x64,
        AnyCPU,
        x86Pref
    }

    public enum MixedDepAction {
        Keep,
        Stub,
        Remove
    }
}
