using System.Collections;
using System.Globalization;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ComboManager : MonoBehaviour
{
    public static ComboManager Instance;
    private PlayerStats playerStats;

    [Header("Sound Effects")]
    //public AudioClip hitSound;
    public AudioClip missSound;
    public AudioSource audioSource;

    [Header("Combo UI")]
    public TextMeshProUGUI comboText;
    public float tapBounceDuration = 0.2f;     //single tap bounce duration
    public float holdBounceDuration = 0.4f;    //full cycle up & down for hold
    public float holdBounceAmplitude = 10f;    //pixels moved up/down for hold
    public float tapBounceAmplitude = 15f;     //pixels moved up/down for tap

    [HideInInspector] public int combo = 0;
    private Vector3 originalPos;
    private Coroutine holdCoroutine;
    private bool isHolding = false;

    private MissEffect cachedMissEffect;

    private ulong playerClientId;

    private void Awake()
    {
        Instance = this;

        cachedMissEffect = FindObjectOfType<MissEffect>();

        if (comboText != null)
            originalPos = comboText.transform.localPosition;

        playerStats = NetworkManager.Singleton.LocalClient?.PlayerObject?.GetComponent<PlayerStats>();

        playerClientId = playerStats.OwnerClientId;
    }

    ///<summary>Starts tap animation</summary>
    public void PlayPopAnimation(Note note)
    {
        if (note == null) return;
        StartCoroutine(TapBounce());
    }

    ///<summary>Starts continuous hold pulse animation</summary>
    public void StartHoldPulse(Note note)
    {
        if (!isHolding)
        {
            isHolding = true;
            holdCoroutine = StartCoroutine(HoldPulse());
        }
    }

    ///<summary>Stops the hold pulse animation</summary>
    public void StopHoldPulse(Note note = null)
    {
        if (holdCoroutine != null)
        {
            StopCoroutine(holdCoroutine);
            holdCoroutine = null;
        }
        isHolding = false;
        if (comboText != null)
            comboText.transform.localPosition = originalPos;
    }

    ///<summary>Increment combo count and trigger animations</summary>
    public void AddCombo(bool isHold, Note note = null)
    {
        if (playerStats.Health.Value <= 0f)
            return;

        combo++;

        //Play Hit SFX - TOO DISTRACTING
        //if (audioSource && hitSound)
        //    audioSource.PlayOneShot(hitSound);

        //Network the combo increase
        if (playerStats != null)
        {
            playerStats.UpdateComboServerRpc(combo);
            playerStats.IncrementVisualComboServerRpc();
        }
        UpdateComboText();

        if (combo < 2) return;

        //Tap animation only if not currently holding
        if (!isHold && !isHolding)
            StartCoroutine(TapBounce());

        //Start hold pulse if this is a hold note
        else if (isHold && !isHolding)
            StartHoldPulse(note);
    }

    ///<summary>Reset combo counter and stop any hold animations</summary>
    public void ResetCombo()
    {
        //Only act if this is the local player's combo
        if (playerStats.OwnerClientId != NetworkManager.Singleton.LocalClientId)
            return;

        //Play Miss SFX
        audioSource.PlayOneShot(missSound);

        //Network health loss
        if (!playerStats.IsOwner)
        {
            Debug.LogWarning("Not owner of PlayerStats - cannot modify health directly");
        }
        else
        {
            float deduction = (combo > 30) ? 2f : 5f;
            float oldHealth = playerStats.Health.Value;
            float newHealth = Mathf.Max(0f, oldHealth - deduction);

            playerStats.Health.Value = newHealth;

            GameplayUI.Instance.ResetComboBarServerRpc(playerClientId);

            //Show damage popup
            if (playerStats != null)
                playerStats.ShowDamagePopupServerRpc(NetworkManager.Singleton.LocalClientId, deduction);
        }

        if (combo > 1)
        {
            ResultsManager.Instance.OnComboBreak();
            //Debug.Log("Registered Combo Break");

            //GameplayUI.Instance.ResetComboBar(playerClientId);

            //Flash Screen
            if (cachedMissEffect != null)
                cachedMissEffect.TriggerMissFlash();
        }

        combo = 0;

        //Network the combo reset
        if (playerStats != null)
        {
            playerStats.UpdateComboServerRpc(0);
        }

        UpdateComboText();
        StopHoldPulse();
    }

    private void UpdateComboText()
    {
        if (!comboText) return;

        comboText.gameObject.SetActive(combo >= 2);
        if (combo >= 2)
            comboText.text = $"Combo: {combo}";
    }

    ///<summary>Short upward then downward movement for tap notes</summary>
    private IEnumerator TapBounce()
    {
        if (!comboText) yield break;

        Vector3 start = originalPos;
        Vector3 end = start + Vector3.up * tapBounceAmplitude;

        float timer = 0f;
        while (timer < tapBounceDuration)
        {
            timer += Time.deltaTime;
            comboText.transform.localPosition = Vector3.Lerp(start, end, timer / tapBounceDuration);
            yield return null;
        }

        timer = 0f;
        while (timer < tapBounceDuration)
        {
            timer += Time.deltaTime;
            comboText.transform.localPosition = Vector3.Lerp(end, start, timer / tapBounceDuration);
            yield return null;
        }

        comboText.transform.localPosition = originalPos;
    }

    ///<summary>Continuous up and down bouncing for hold notes</summary>
    private IEnumerator HoldPulse()
    {
        if (!comboText) yield break;

        float halfCycle = holdBounceDuration / 2f;

        while (isHolding)
        {
            //Move up
            float timer = 0f;
            Vector3 start = originalPos;
            Vector3 end = originalPos + Vector3.up * holdBounceAmplitude;

            while (timer < halfCycle && isHolding)
            {
                timer += Time.deltaTime;
                comboText.transform.localPosition = Vector3.Lerp(start, end, timer / halfCycle);
                yield return null;
            }

            //Move down
            timer = 0f;
            while (timer < halfCycle && isHolding)
            {
                timer += Time.deltaTime;
                comboText.transform.localPosition = Vector3.Lerp(end, start, timer / halfCycle);
                yield return null;
            }
        }

        //Reset position after stopping
        if (comboText != null)
            comboText.transform.localPosition = originalPos;
    }
}