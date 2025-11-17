using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections.Generic;

public class GameplayUI : NetworkBehaviour
{
    public static GameplayUI Instance { get; private set; }

    [Header("Player 1 - Left Side")]
    public Slider leftComboSlider;
    public Slider leftHealthSlider;

    [Header("Player 2 - Right Side")]
    public Slider rightComboSlider;
    public Slider rightHealthSlider;

    private Dictionary<ulong, int> playerSide = new();

    public override void OnNetworkSpawn()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupSlider(leftComboSlider, 100f, 0f);
        SetupSlider(leftHealthSlider, 100f, 100f);
        SetupSlider(rightComboSlider, 100f, 0f);
        SetupSlider(rightHealthSlider, 100f, 100f);
    }

    private void SetupSlider(Slider slider, float maxValue, float startValue)
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = maxValue;
            slider.value = startValue;
            slider.interactable = false;
        }
    }

    ///<summary>Called from PlayerStats when values change</summary>
    public void UpdatePlayer(ulong clientId, int combo, float health)
    {
        int side = GetPlayerSide(clientId);

        //Combo: 1:1 fill (combo 100 = full bar) TO BE CHANGED
        if (side == 0)
        {
            leftComboSlider.value = combo;
            leftHealthSlider.value = health;
        }
        else
        {
            rightComboSlider.value = combo;
            rightHealthSlider.value = health;
        }
    }

    private int GetPlayerSide(ulong clientId)
    {
        if (!playerSide.ContainsKey(clientId))
        {
            //First player = left, second = right
            int index = playerSide.Count;
            playerSide[clientId] = index;
        }
        return playerSide[clientId];
    }
}