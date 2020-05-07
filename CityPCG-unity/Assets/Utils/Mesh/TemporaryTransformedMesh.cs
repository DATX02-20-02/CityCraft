using UnityEngine;

public class TemporaryTransformedMesh {

    public readonly Matrix4x4 transform;
    public readonly GameObject gameObject;

    public TemporaryTransformedMesh(Matrix4x4 transform, GameObject gameObject) {
        this.transform = transform;
        this.gameObject = gameObject;
    }
}
