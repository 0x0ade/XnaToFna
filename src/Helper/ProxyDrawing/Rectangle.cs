using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XnaToFna.ProxyDrawing {
    [Serializable]
    public struct Rectangle {

        public readonly static Rectangle Empty;

        private int x;
        private int y;
        private int width;
        private int height;

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

        public int Width {
            get {
                return width;
            }
            set {
                width = value;
            }
        }

        public int Height {
            get {
                return height;
            }
            set {
                height = value;
            }
        }

        public int Left {
            get {
                return X;
            }
        }

        public int Top {
            get {
                return y;
            }
        }

        public int Right {
            get {
                return X + Width;
            }
        }

        public int Bottom {
            get {
                return y + height;
            }
        }

        public Rectangle(int x, int y, int width, int height) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public void Inflate(int width, int height) {
            x -= width;
            y -= height;
            this.width += width * 2;
            this.height += height * 2;
        }

        public void Intersect(Rectangle rect) {
            if (!IntersectsWithInclusive(rect)) {
                x = y = width = height = 0;
                return;
            }

            x = Math.Max(Left, rect.Left);
            y = Math.Max(Top, rect.Top);
            width = Math.Min(Right, rect.Right) - x;
            height = Math.Min(Bottom, rect.Bottom) - y;
        }

        public bool Contains(int x, int y)
            => x >= Left &&
            x < Right &&
            y >= Top &&
            y < Bottom;

        public bool Contains(Rectangle rect) {
            return rect == Intersect(this, rect);
        }

        public bool IntersectsWith(Rectangle rect)
            => !(
                Left >= rect.Right ||
                Right <= rect.Left ||
                Top >= rect.Bottom ||
                Bottom <= rect.Top
            );

        private bool IntersectsWithInclusive(Rectangle rect)
            => !(
                Left > rect.Right ||
                Right < rect.Left ||
                Top > rect.Bottom ||
                Bottom < rect.Top
            );

        public void Offset(int x, int y) {
            this.x += x;
            this.y += y;
        }

        public static Rectangle FromLTRB(int left, int top, int right, int bottom)
            => new Rectangle(left, top, right - left, bottom - top);

        public static Rectangle Inflate(Rectangle rect, int x, int y) {
            Rectangle r = new Rectangle(rect.x, rect.y, rect.width, rect.height);
            r.Inflate(x, y);
            return r;
        }

        public static Rectangle Intersect(Rectangle a, Rectangle b) {
            a = new Rectangle(a.x, a.y, a.width, a.height);
            a.Intersect(b);
            return a;
        }

        public static Rectangle Union(Rectangle a, Rectangle b)
            => FromLTRB(
                Math.Min(a.Left, b.Left),
                Math.Min(a.Top, b.Top),
                Math.Max(a.Right, b.Right),
                Math.Max(a.Bottom, b.Bottom)
            );

        public static bool operator !=(Rectangle left, Rectangle right)
            => !(left == right);

        public static bool operator ==(Rectangle left, Rectangle right)
            =>  left.x == right.x &&
            left.y == right.y &&
            left.width == right.width &&
            left.height == right.height;

        public override bool Equals(object obj)
            => obj is Rectangle && this == (Rectangle) obj;

        public override int GetHashCode()
            => (height + width) ^ x + y;

        public override string ToString()
            => $"{{X={x},Y={y},Width={width},Height={height}}}";

    }
}
