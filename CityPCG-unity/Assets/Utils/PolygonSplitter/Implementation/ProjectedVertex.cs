using UnityEngine;

namespace Utils.PolygonSplitter.Implementation {

    /**
     * A vertex projected on the given line segment
     */
    public class ProjectedVertex {
        public readonly bool valid;
        public readonly Vector2 vertex;

        public static readonly ProjectedVertex INVALID = new ProjectedVertex();

        private readonly LineSegment edge;

        private ProjectedVertex() {
            valid = false;
            edge = null;
            vertex = Vector2.zero;
        }

        public ProjectedVertex(Vector2 vertex, LineSegment edge) {
            this.edge = edge;
            valid = true;
            this.vertex = vertex;
        }

        public bool IsOnEdge(LineSegment edge) {
            return valid && this.edge.EqualsTopo(edge);
        }

    }
}
