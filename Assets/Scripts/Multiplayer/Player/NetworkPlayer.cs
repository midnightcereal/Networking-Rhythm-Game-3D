using UnityEngine;
using Unity.Netcode;

public class NetworkPlayer : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        LobbyManager.Instance?.PlayerSpawned(this);
    }

    public override void OnNetworkDespawn()
    {
        LobbyManager.Instance?.PlayerDespawned(this);
    }
}