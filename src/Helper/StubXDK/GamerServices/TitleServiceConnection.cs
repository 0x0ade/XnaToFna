using Microsoft.Xna.Framework;
using MonoMod;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.StubXDK.GamerServices {
    public class TitleServiceConnection : IDisposable {

        private readonly int ServiceId;
        private readonly TitleServiceDescription Description;

        public event EventHandler<AsyncCompletedEventArgs> ConnectCompleted;

        public TitleServiceConnectionStatus Status {
            get {
                return 0;
            }
        }

        public TitleServiceConnection(int serviceId, TitleServiceDescription description) {
            ServiceId = serviceId;
            Description = description;
        }

        public void ConnectAsync() {

        }

        public HttpWebRequest CreateWebRequest(int port, Uri uri) {
            if (port != 80) {
                // Not what MXF.Xdk would do, but hey...
                throw new NotSupportedException("Creating HTTP requests for port != 80 not supported");
            }
            return WebRequest.CreateHttp(uri);
        }

        public void Dispose() {
            // No-op.
        }

    }
}
