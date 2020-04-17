using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Original implementation from Paul Salaets in the Node.JS package "line-intersect"
// https://github.com/psalaets/line-intersect
// Ported to C# and modified to also include ray-line intersection
public class LineIntersection {
    public enum Type {
        None,
        Intersecting,
        Colinear,
        Parallel
    }

    public class Result {
        public Type type;
        public Vector2 origin;
        public Vector2 point;
        public float factorA;
        public float factorB;

        public Result(Type type) {
            this.type = type;
        }

        public Result(Type type, Vector2 origin, Vector2 point, float factorA, float factorB) : this(type) {
            this.origin = origin;
            this.point = point;
            this.factorA = factorA;
            this.factorB = factorB;
        }
    }

    public static Result RayTest(Vector2 fromA, Vector2 toA, Vector2 origin, Vector2 dir) {
        float denom = (dir.y * (toA.x - fromA.x)) - (dir.x * (toA.y - fromA.y));
        float numeA = (dir.x * (fromA.y - origin.y)) - (dir.y * (fromA.x - origin.x));
        float numeB = ((toA.x - fromA.x) * (fromA.y - origin.y)) - ((toA.y - fromA.y) * (fromA.x - origin.x));

        if (denom == 0) {
            if (numeA == 0 && numeB == 0) {
                return new Result(Type.Colinear);
            }
            return new Result(Type.Parallel);
        }

        float uA = numeA / denom;
        float uB = numeB / denom;

        if (uA >= 0 && uA <= 1 && uB >= 0) {
            return new Result(Type.Intersecting, origin, origin + uB * dir, uA, uB);
        }

        return new Result(Type.None);
    }

    public static Result LineTest(Vector2 fromA, Vector2 toA, Vector2 fromB, Vector2 toB) {
        Result res = RayTest(fromA, toA, fromB, toB - fromB);

        if (res.factorB <= 1) {
            return res;
        }

        return new Result(Type.None);
    }
}
