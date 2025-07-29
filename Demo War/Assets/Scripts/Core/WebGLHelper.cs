using UnityEngine;

/// <summary>
/// Helper class for WebGL specific functionality
/// </summary>
public static class WebGLHelper
{
    /// <summary>
    /// Triggers haptic feedback on supported devices
    /// </summary>
    /// <param name="type">Type of haptic feedback (light, medium, heavy)</param>
    public static void TriggerHapticFeedback(string type)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            // ��� WebGL ����� ������������ JavaScript interop
            Application.ExternalCall("triggerHapticFeedback", type);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Haptic feedback not supported: {e.Message}");
        }
#elif UNITY_ANDROID || UNITY_IOS
        // ��� ��������� �������� ����� ������������ Handheld.Vibrate()
        if (type == "light")
        {
            Handheld.Vibrate(); // �������� ��������
        }
        else if (type == "medium" || type == "heavy")
        {
            // ����� ����������� ������ ������������� ����� ��������
            Handheld.Vibrate();
        }
#else
        // ��� ������ �������� ������ ��������
        Debug.Log($"Haptic feedback requested: {type}");
#endif
    }

    /// <summary>
    /// Checks if the game is running in WebGL
    /// </summary>
    public static bool IsWebGL()
    {
#if UNITY_WEBGL
        return true;
#else
        return false;
#endif
    }

    /// <summary>
    /// Gets the current platform name
    /// </summary>
    public static string GetPlatformName()
    {
#if UNITY_WEBGL
        return "WebGL";
#elif UNITY_ANDROID
        return "Android";
#elif UNITY_IOS
        return "iOS";
#elif UNITY_STANDALONE_WIN
        return "Windows";
#elif UNITY_STANDALONE_OSX
        return "macOS";
#elif UNITY_STANDALONE_LINUX
        return "Linux";
#else
        return "Unknown";
#endif
    }

    /// <summary>
    /// JavaScript function for WebGL haptic feedback
    /// Add this to your index.html in WebGL build:
    /// 
    /// <script>
    /// function triggerHapticFeedback(type) {
    ///     if (navigator.vibrate) {
    ///         switch(type) {
    ///             case 'light': navigator.vibrate(50); break;
    ///             case 'medium': navigator.vibrate(100); break;
    ///             case 'heavy': navigator.vibrate(200); break;
    ///         }
    ///     }
    /// }
    /// </script>
    /// </summary>
    public static void LogWebGLInstructions()
    {
        Debug.Log("To enable haptic feedback in WebGL, add the JavaScript function to your index.html");
    }
}