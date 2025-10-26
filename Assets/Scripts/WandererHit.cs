using System.Collections;
using UnityEngine;

public class WandererHit : MonoBehaviour
{
    public float flashDuration = 0.25f;    // how long the red flash lasts
    public Color hitColor = Color.red;

    private Renderer rend;
    private Color originalColor;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            originalColor = rend.material.color; // this creates a material instance
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Projectile"))
        {
            // Start flash
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        // Safety check
        if (rend == null) yield break;
        // Flash to hit color
        rend.material.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        rend.material.color = originalColor;
    }
}
