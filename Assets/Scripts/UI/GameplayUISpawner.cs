using UnityEngine;
using Unity.Netcode;

public class GameplayUISpawner : NetworkBehaviour
{
    public GameObject uiPrefab;
    private bool hasSpawnedUI = false;

    public override void OnNetworkSpawn()
    {
        //Only server/host spawns the UI
        if (!IsServer)
        {
            Debug.Log($"Client {OwnerClientId} - Not server, skipping UI spawn.");
            return;
        }

        if (uiPrefab == null)
        {
            Debug.LogError("uiPrefab is NULL!");
            return;
        }

        if (!uiPrefab.GetComponent<NetworkObject>())
        {
            Debug.LogError("uiPrefab is missing NetworkObject component!");
            return;
        }

        Debug.Log("Server is spawning UI prefab");
    }

    //Called From SongIntroUI After Intro
    public void SpawnAfterIntro()
    {
        Debug.Log("Called UI Spawn After Intro");

        if (!IsServer) return;
        if (hasSpawnedUI)
        {
            Debug.Log("UI already spawned, skipping.");
            return;
        }

        Debug.Log("Song intro finished -> Spawning Gameplay UI NOW!");
        SpawnGameplayUI();
    }

    private void SpawnGameplayUI()
    {
        if (hasSpawnedUI) return;

        GameObject ui = Instantiate(uiPrefab, Vector3.zero, Quaternion.identity);
        var netObj = ui.GetComponent<NetworkObject>();

        if (netObj != null)
        {
            netObj.Spawn();
            hasSpawnedUI = true;
            Debug.Log("Gameplay UI spawned and synced to all clients");
        }
        else
        {
            Debug.LogError("Spawned UI has no NetworkObject!");
        }
    }
}