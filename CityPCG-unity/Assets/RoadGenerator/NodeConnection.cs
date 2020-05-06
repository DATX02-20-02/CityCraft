public enum ConnectionType {
    None,
    Main,
    Highway,
    Street,
    ParkPath
};

public class NodeConnection {
    public Node node;
    public ConnectionType type;

    public NodeConnection(Node node, ConnectionType type) {
        this.node = node;
        this.type = type;
    }
}
