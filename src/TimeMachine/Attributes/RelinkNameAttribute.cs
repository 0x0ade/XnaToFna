using MonoMod;
using System;

namespace XnaToFna.TimeMachine {
    public class RelinkNameAttribute : Attribute {
        public string Name;
        public RelinkNameAttribute(string name) {
            Name = name;
        }
    }
}
