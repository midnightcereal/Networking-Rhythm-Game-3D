using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;
    public Image healthBar;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void HitNote()
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + 5f);
        UpdateUI();
    }

    public void MissNote()
    {
        currentHealth -= 10f;
        UpdateUI();
        if (currentHealth <= 0)
        {
            Debug.Log("Player failed!");
        }
    }

    void UpdateUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }
}