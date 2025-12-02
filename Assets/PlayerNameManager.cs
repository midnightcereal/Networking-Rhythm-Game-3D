using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerNameManager : MonoBehaviour
{
    public static PlayerNameManager Instance { get; private set; }

    public static event Action<ulong, string> OnPlayerNameReceived;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void BroadcastPlayerName(ulong clientId, string name)
    {
        Debug.Log("GAMEPLAYUI Broadcasted player name");
        OnPlayerNameReceived?.Invoke(clientId, name);
    }
}