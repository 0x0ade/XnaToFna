using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public class ForcedStreamContentManager : ContentManager {

        public Stream Stream;

        public ForcedStreamContentManager(IServiceProvider serviceProvider)
            : base(serviceProvider) {
        }

        protected override Stream OpenStream(string assetName) {
            bool isXTF = assetName.StartsWith("///XNATOFNA/");
            if (isXTF)
                assetName = assetName.Substring(17); // Removes //XNATOFNA/abcd/

            Stream stream;
            if (isXTF && Stream != null) {
                stream = Stream;
                Stream = null;
            } else {
                stream = base.OpenStream(assetName);
            }
            return stream;
        }

    }
}
