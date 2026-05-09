using Unity.Netcode.Components;

/// <summary>
/// Owner-authoritative NetworkTransform.
/// The owner (client) controls their own position/rotation, and changes are
/// replicated to the server and all other clients.
/// Used for player prefabs so clients can move freely without server-side rollback.
/// </summary>
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
