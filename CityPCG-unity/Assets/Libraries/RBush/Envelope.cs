using System;
using System.Numerics;

namespace RBush {
    public readonly struct Envelope : IEquatable<Envelope> {
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }

        public double Area => Math.Max(this.MaxX - this.MinX, 0) * Math.Max(this.MaxY - this.MinY, 0);
        public double Margin => Math.Max(this.MaxX - this.MinX, 0) + Math.Max(this.MaxY - this.MinY, 0);

        public Envelope(double minX, double minY, double maxX, double maxY) {
            this.MinX = minX;
            this.MinY = minY;
            this.MaxX = maxX;
            this.MaxY = maxY;
        }

        public Envelope Extend(in Envelope other) =>
            new Envelope(
                minX: Math.Min(this.MinX, other.MinX),
                minY: Math.Min(this.MinY, other.MinY),
                maxX: Math.Max(this.MaxX, other.MaxX),
                maxY: Math.Max(this.MaxY, other.MaxY));

        public Envelope Clone() =>
            new Envelope(this.MinX, this.MinY, this.MaxX, this.MaxY);

        public Envelope Intersection(in Envelope other) =>
            new Envelope(
                minX: Math.Max(this.MinX, other.MinX),
                minY: Math.Max(this.MinY, other.MinY),
                maxX: Math.Min(this.MaxX, other.MaxX),
                maxY: Math.Min(this.MaxY, other.MaxY)
            );

        public bool Contains(in Envelope other) =>
            this.MinX <= other.MinX &&
            this.MinY <= other.MinY &&
            this.MaxX >= other.MaxX &&
            this.MaxY >= other.MaxY;

        public bool Intersects(in Envelope other) =>
            this.MinX <= other.MaxX &&
            this.MinY <= other.MaxY &&
            this.MaxX >= other.MinX &&
            this.MaxY >= other.MinY;

        public bool Equals(Envelope other) =>
            this == other;

        private int CombineHashCodes(int h1, int h2) {
            return (((h1 << 5) + h1) ^ h2);
        }

        public override int GetHashCode() {
            int hash = this.MinX.GetHashCode();
            hash = CombineHashCodes(hash, this.MinY.GetHashCode());
            hash = CombineHashCodes(hash, this.MaxX.GetHashCode());
            hash = CombineHashCodes(hash, this.MaxY.GetHashCode());
            return hash;
        }

        public override bool Equals(Object obj) {
            //Check for null and compare run-time types.
            if((obj == null) || !this.GetType().Equals(obj.GetType())) {
                return false;
            }
            else {
                Envelope e = (Envelope)obj;
                return MinX == e.MinX &&
                    MinY == e.MinY &&
                    MaxX == e.MaxX &&
                    MaxY == e.MaxY;
            }
        }

        public static bool operator ==(Envelope left, Envelope right) => left.Equals(right);
        public static bool operator !=(Envelope left, Envelope right) => !left.Equals(right);

        public static Envelope InfiniteBounds { get; } =
            new Envelope(
                minX: double.NegativeInfinity,
                minY: double.NegativeInfinity,
                maxX: double.PositiveInfinity,
                maxY: double.PositiveInfinity);

        public static Envelope EmptyBounds { get; } =
            new Envelope(
                minX: double.PositiveInfinity,
                minY: double.PositiveInfinity,
                maxX: double.NegativeInfinity,
                maxY: double.NegativeInfinity);
    }
}
