using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shaker : Singleton<Shaker>
{
  
  public static IEnumerator ShakeCoroutine(Transform transform, float _shakeDuration = 0.5f, float _shakeIntensity = 10f)
  {
    float elapsed = 0f;
   
    Vector3 _originalLocalPosition = transform.localPosition;
    
    while (elapsed < _shakeDuration)
    {
      // Calculate random offset for shake
      float offsetX = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
      float offsetY = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
            
      // Apply shake offset
      transform.localPosition = _originalLocalPosition + new Vector3(offsetX, offsetY, 0f);
            
      elapsed += Time.deltaTime;
      yield return null;
    }
        
    // Reset to original position
    transform.localPosition = _originalLocalPosition;
  }
}
