using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonParticle : MonoBehaviour
{
    [Header("Particle System")]
    public ParticleSystem targetParticleSystem;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null && targetParticleSystem != null)
        {
            button.onClick.AddListener(PlayParticle);
        }
        else
        {
            if (button == null)
                Debug.LogError("No Button component found on this GameObject", gameObject);

            if (targetParticleSystem == null)
                Debug.LogError("No ParticleSystem assigned in the Inspector", gameObject);
        }
    }

    private void PlayParticle()
    {
        if (targetParticleSystem != null)
        {
            targetParticleSystem.Play();
            Debug.Log($"[ButtonParticle] Played {targetParticleSystem.name}");
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayParticle);
    }
}