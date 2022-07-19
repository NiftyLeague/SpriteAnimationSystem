using Unity.Collections;
using UnityEditor;


public static class LeakDetection
{
	[MenuItem("Tools/Leak Detection/Enable")]
	private static void EnableLeakDetection()
	{
		NativeLeakDetection.Mode = NativeLeakDetectionMode.Enabled;
	}

	[MenuItem("Tools/Leak Detection/Enable With Stack Trace")]
	private static void EnableLeakDetectionWithStackTrace()
	{
		NativeLeakDetection.Mode = NativeLeakDetectionMode.EnabledWithStackTrace;
	}

	[MenuItem("Tools/Leak Detection/Disable")]
	private static void DisableLeakDetection()
	{
		NativeLeakDetection.Mode = NativeLeakDetectionMode.Disabled;
	}
}
