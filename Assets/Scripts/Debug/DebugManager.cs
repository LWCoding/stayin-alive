using UnityEngine;

/// <summary>
/// Debug manager that provides debug functionality, such as speeding up time.
/// When LeftShift is held, time scale is set to 3x. When released, returns to normal.
/// </summary>
public class DebugManager : Singleton<DebugManager>
{
	private const float NORMAL_TIME_SCALE = 1f;
	private const float FAST_TIME_SCALE = 3f;

	private void Update()
	{
		// Check if LeftShift is held
		if (Input.GetKey(KeyCode.LeftShift))
		{
			Time.timeScale = FAST_TIME_SCALE;
		}
		else
		{
			Time.timeScale = NORMAL_TIME_SCALE;
		}
	}

	private new void OnDestroy()
	{
		// Ensure time scale is reset to normal when DebugManager is destroyed
		Time.timeScale = NORMAL_TIME_SCALE;
		base.OnDestroy();
	}
}

