namespace Utils.PolygonSplitter {
    public class Cut {
        public readonly float length;
        public readonly Polygon cutAway;

        public Cut(float length, Polygon cutAway) {
            this.cutAway = cutAway;
            this.length = length;
        }

        public override string ToString() {
            return length + ": " + cutAway;
        }
    }
}
