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

    public static NetworkPlayer LocalPlayerInstance { get; private set; }

    public override void OnNetworkSpawn()
    {
        //Only the owner sets their display name
        if (IsOwner)
        {
            LocalPlayerInstance = this;

            //Load name from LobbyUI or PlayerPrefs
            string customName = LobbyUI.Instance != null ? LobbyUI.Instance.GetCurrentPlayerName() : (IsHost ? "Host" : "Client");

            if (string.IsNullOrWhiteSpace(customName))
                customName = IsHost ? "Host" : "Client";

            DisplayName.Value = new FixedString128Bytes(customName);
        }

        //Subscribe to changes so UI updates automatically
        DisplayName.OnValueChanged += (oldValue, newValue) =>
        {
            LobbyManager.Instance?.RebuildLobbyUI();
        };

        //Add this player to the lobby manager list
        LobbyManager.Instance?.PlayerSpawned(this);
        //Update UI immediately
        LobbyManager.Instance?.RebuildLobbyUI();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
            LocalPlayerInstance = null;

        //Remove this player from the lobby manager list
        LobbyManager.Instance?.PlayerDespawned(this);
        //Update UI
        LobbyManager.Instance?.RebuildLobbyUI();
    }

    public void SetDisplayName(string newName)
    {
        if (IsOwner && !string.IsNullOrWhiteSpace(newName))
        {
            DisplayName.Value = new FixedString128Bytes(newName);
        }
    }
}