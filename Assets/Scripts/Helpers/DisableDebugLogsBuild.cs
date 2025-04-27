using UnityEngine;

namespace Helpers
{
    public class DisableDebugLogsBuild : MonoBehaviour
    {
        // Taken from tutorial available on https://www.youtube.com/watch?v=XRr9GqldlPY
    
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void DisableLoggerOutsideOfEditor()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.unityLogger.logEnabled = true;
#else
        Debug.unityLogger.logEnabled = false;
#endif
        }
    }
}
