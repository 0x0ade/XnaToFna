using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna {
    public class CopyingStream : Stream {

        public Stream Input;
        public bool LeaveInputOpen;
        public Stream Output;
        public bool LeaveOutputOpen;
        public bool Copy = true;

        public override bool CanRead {
            get {
                return Input.CanRead;
            }
        }

        public override bool CanSeek {
            get {
                return Input.CanSeek;
            }
        }

        public override bool CanWrite {
            get {
                return Input.CanWrite;
            }
        }

        public override long Length {
            get {
                return Input.Length;
            }
        }

        public override long Position {
            get {
                return Input.Position;
            }

            set {
                Seek(value, SeekOrigin.Begin);
            }
        }

        public CopyingStream(Stream input, Stream output)
            : this(input, false, output, false) {
        }
        public CopyingStream(Stream input, bool leaveInputOpen, Stream output, bool leaveOutputOpen) {
            Input = input;
            LeaveInputOpen = leaveInputOpen;
            Output = output;
            LeaveOutputOpen = leaveOutputOpen;
        }

        public override void Flush() {
            Input.Flush();
            if (Copy)
                Output.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            int read = Input.Read(buffer, offset, count);
            if (Copy)
                Output.Write(buffer, offset, read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            if (!Copy)
                return Input.Seek(offset, origin);

            long target = 0;
            switch (origin) {
                case SeekOrigin.Begin:
                    target = offset;
                    break;
                case SeekOrigin.Current:
                    target = Position + offset;
                    break;
                case SeekOrigin.End:
                    target = Input.Length - offset;
                    break;
            }

            if (target == Position)
                // no-op.
                return Position;

            if (target < Position) {
                // Seek both streams.
                Output.Seek(target, SeekOrigin.Begin);
                return Input.Seek(offset, origin);
            }

            // Read into temporary buffer, write into output.
            byte[] buffer = new byte[offset - Position];
            int read = 0;
            while (read < buffer.Length)
                read += Input.Read(buffer, read, buffer.Length - read);
            Output.Write(buffer, 0, buffer.Length);
            return Position;
        }

        public override void SetLength(long value) {
            Input.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            Input.Write(buffer, offset, count);
            if (Copy)
                Output.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing) {
            if (!LeaveInputOpen)
                Input.Dispose();
            if (!LeaveOutputOpen)
                Output.Dispose();
            base.Dispose(disposing);
        }

    }
}
