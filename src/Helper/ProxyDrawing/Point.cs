using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.ProxyDrawing {
    [Serializable]
    public struct Point {

        public readonly static Point Empty;

        private int x;
        private int y;

        public bool IsEmpty {
            get {
                return this == Empty;
            }
        }

        public int X {
            get {
                return x;
            }
            set {
                x = value;
            }
        }

        public int Y {
            get {
                return y;
            }
            set {
                y = value;
            }
        }

        public Point(int dw) 
            : this(dw & 0xFFFF, dw >> 16) { }
        public Point(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void Offset(Point p)
            => Offset(p.X, p.Y);
        public void Offset(int x, int y) {
            this.x += x;
            this.y += y;
        }

        public static bool operator !=(Point left, Point right)
            => !(left == right);

        public static bool operator ==(Point left, Point right)
            =>  left.x == right.x &&
            left.y == right.y;

        public override bool Equals(object obj)
            => obj is Point && this == (Point) obj;

        public override int GetHashCode()
            => x ^ y;

        public override string ToString()
            => $"{{X={x},Y={y}}}";

    }
}
