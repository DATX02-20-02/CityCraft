namespace Utils.PolygonSplitter.Implementation {
    /**
     * Represents a cut from a polygon. The cut is a completely inside the parent polygon.
     *
     * Heavily inspired by the project Polysplit made by Gediminas Rimša, read more in license.txt.
     */
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
