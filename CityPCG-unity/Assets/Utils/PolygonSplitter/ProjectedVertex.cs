using System.Numerics;
using Vector3 = UnityEngine.Vector3;

namespace Utils.PolygonSplitter {
    public class ProjectedVertex {
        public readonly LineSegment edge;
        public readonly bool valid;
        public readonly Vector3 vertex;

        public static readonly ProjectedVertex INVALID = new ProjectedVertex();

        private ProjectedVertex() {
            this.valid = false;
            this.edge = null;
            this.vertex = Vector3.zero;
        }

        public ProjectedVertex(Vector3 vertex, LineSegment edge) {
            this.edge = edge;
            this.valid = true;
            this.vertex = vertex;
        }

        public bool IsOnEdge(LineSegment edge) {
            return valid && this.edge.EqualsTopo(edge);
        }

    }
}
