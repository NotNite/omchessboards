namespace Firehorse.Chess;

// This SUCKS lol
public class ConnectionManager {
    private readonly List<Connection> connections = [];

    public void AddConnection(Connection connection) {
        lock (this.connections) this.connections.Add(connection);
    }

    public void RemoveConnection(Connection connection) {
        lock (this.connections) this.connections.Remove(connection);
    }

    public Connection GetRandomConnection() {
        lock (this.connections) {
            if (this.connections.Count == 0) throw new Exception("No connections available");
            var idx = Random.Shared.Next(this.connections.Count);
            return this.connections[idx];
        }
    }
}
