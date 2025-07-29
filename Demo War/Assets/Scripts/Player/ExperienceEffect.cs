using UnityEngine;

public class EffectAnimator : MonoBehaviour
{
    private SpriteRenderer renderer;
    private float duration = 0.15f;
    private float timer = 0f;
    private Vector3 startScale;

    public void StartAnimation(SpriteRenderer effectRenderer)
    {
        renderer = effectRenderer;
        startScale = transform.localScale;
        StartCoroutine(AnimateAndDestroy());
    }

    private System.Collections.IEnumerator AnimateAndDestroy()
    {
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            transform.localScale = startScale * (1f + progress);
            var color = renderer.color;
            color.a = 1f - progress;
            renderer.color = color;
            yield return null;
        }

        Destroy(gameObject);
    }
}