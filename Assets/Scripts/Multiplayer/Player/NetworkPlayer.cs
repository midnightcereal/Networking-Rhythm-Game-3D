using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public NetworkVariable<FixedString128Bytes> DisplayName = new(
        new FixedString128Bytes(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        LobbyManager.Instance?.PlayerSpawned(this);

        if (IsOwner)
        {
            string name = IsHost ? "Host" : "Client";
            DisplayName.Value = new FixedString128Bytes(name);
        }
    }
}