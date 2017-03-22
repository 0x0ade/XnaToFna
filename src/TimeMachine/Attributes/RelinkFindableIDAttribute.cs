using MonoMod;
using System;

namespace XnaToFna.TimeMachine {
    public class RelinkFindableIDAttribute : Attribute {
        public string FindableID;
        public RelinkFindableIDAttribute(string findableID) {
            FindableID = findableID;
        }
    }
}
