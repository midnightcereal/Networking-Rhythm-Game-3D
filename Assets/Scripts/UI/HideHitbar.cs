using System.Collections;
using UnityEngine;

public class HideHitbar : MonoBehaviour
{
    public static HideHitbar Instance;

    private Coroutine hideRoutine;

    private void Awake()
    {
        Instance = this;
    }

    ///<summary>Called from ComboAbilityManager -> Hides the hitbar for x duration and restores it afterwards</summary>
    public void HideForDuration(float duration)
    {
        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideRoutine(duration));
    }

    private IEnumerator HideRoutine(float duration)
    {
        gameObject.GetComponent<MeshRenderer>().enabled = false;

        yield return new WaitForSeconds(duration);

        gameObject.GetComponent<MeshRenderer>().enabled = true;
    }
}