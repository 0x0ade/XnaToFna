using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XnaToFna {
    public sealed class SyncResult : IAsyncResult {

        private object _AsyncState;
        public object AsyncState {
            get {
                return _AsyncState;
            }
        }

        public WaitHandle AsyncWaitHandle {
            get {
                return null;
            }
        }

        public bool CompletedSynchronously {
            get {
                return true;
            }
        }

        public bool IsCompleted {
            get {
                return true;
            }
        }

        public SyncResult(object state) {
            _AsyncState = state;
        }

    }
}
