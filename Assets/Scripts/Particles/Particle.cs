using UnityEngine;
using System.Collections;

/// <summary>
/// Component that handles individual particle behavior: movement, fading, and lifecycle.
/// </summary>
public class Particle : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private Vector2 _velocity;
    private float _fadeDelay;
    private float _lifetime;
    private Coroutine _fadeCoroutine;

    /// <summary>
    /// Initializes the particle with sprite, velocity, and fade delay.
    /// </summary>
    public void Initialize(Sprite sprite, Vector2 velocity, float fadeDelay)
    {
        // Get or add SpriteRenderer
        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        // Set sprite
        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = Color.white;
        _spriteRenderer.sortingOrder = 100; // High sorting order to appear on top

        // Store parameters
        _velocity = velocity;
        _fadeDelay = fadeDelay;
        _lifetime = 0f;

        // Start fade coroutine
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeCoroutine());
    }

    private void Update()
    {
        // Move particle
        transform.position += (Vector3)(_velocity * Time.deltaTime);
        _lifetime += Time.deltaTime;
    }

    /// <summary>
    /// Coroutine that handles particle fading after the fade delay.
    /// </summary>
    private IEnumerator FadeCoroutine()
    {
        // Wait for fade delay
        yield return new WaitForSeconds(_fadeDelay);

        // Fade out over 0.5 seconds
        float fadeDuration = 0.5f;
        float elapsed = 0f;
        Color startColor = _spriteRenderer.color;

        while (elapsed < fadeDuration && _spriteRenderer != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            _spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        // Return to pool
        ReturnToPool();
    }

    /// <summary>
    /// Returns this particle to the pool.
    /// </summary>
    private void ReturnToPool()
    {
        if (ParticleEffectManager.Instance != null)
        {
            ParticleEffectManager.Instance.ReturnParticle(this);
        }
        else
        {
            // Fallback: destroy if manager doesn't exist
            Destroy(gameObject);
        }
    }

    private void OnDisable()
    {
        // Stop coroutine if object is disabled
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }
}


