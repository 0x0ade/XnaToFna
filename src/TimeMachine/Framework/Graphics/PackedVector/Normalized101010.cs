#pragma warning disable CS3021 // Type or member does not need a CLSCompliant attribute because the assembly does not have a CLSCompliant attribute
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace XnaToFna.TimeMachine.Framework.Graphics.PackedVector {
    [RelinkType]
    public struct Normalized101010 : IPackedVector<uint>, IEquatable<Normalized101010> {

        private uint packedValue;

        [CLSCompliant(false)]
        public uint PackedValue {
            get {
                return packedValue;
            }
            set {
                packedValue = value;
            }
        }

        public Normalized101010(Vector3 vector)
            : this(
                vector.X,
                vector.Y,
                vector.Z
            ) {
        }

        public Normalized101010(float x, float y, float z) {
            packedValue = Pack(x, y, z);
        }

        public static bool operator !=(Normalized101010 a, Normalized101010 b)
            => !a.Equals(b);

        public static bool operator ==(Normalized101010 a, Normalized101010 b)
            => a.Equals(b);

        public bool Equals(Normalized101010 other)
            => packedValue == other.packedValue;

        public override bool Equals(object obj)
            => obj is Normalized101010 && this.Equals((Normalized101010) obj);

        public override int GetHashCode()
            => (int) packedValue;

        public override string ToString()
            => packedValue.ToString("X");

        public Vector3 ToVector3() {
            Vector4 v4 = Unpack(packedValue);
            return new Vector3(
                v4.X,
                v4.Y,
                v4.Z
            );
        }

        void IPackedVector.PackFromVector4(Vector4 vector) {
            throw new Exception("The method or operation is not implemented.");
        }

        Vector4 IPackedVector.ToVector4()
            => Unpack(packedValue);

        // TODO: [TimeMachine] Normalized101010 (technically 1010102) right now is just Rgba1010102.

        private static uint Pack(float x, float y, float z, float w = 1f)
            => (
                ((uint) Math.Round(MathHelper.Clamp(x, 0, 1) * 1023f)) |
                ((uint) Math.Round(MathHelper.Clamp(y, 0, 1) * 1023f) << 10) |
                ((uint) Math.Round(MathHelper.Clamp(z, 0, 1) * 1023f) << 20) |
                ((uint) Math.Round(MathHelper.Clamp(w, 0, 1) * 3.0f) << 30)
            );

        private static Vector4 Unpack(uint packedValue)
            => new Vector4(
                 (packedValue           & 0x03FF)   / 1023.0f,
                ((packedValue >> 10)    & 0x03FF)   / 1023.0f,
                ((packedValue >> 20)    & 0x03FF)   / 1023.0f,
                 (packedValue >> 30)                / 3.0f
            );

    }
}
