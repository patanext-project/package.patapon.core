using UnityEditor;
using UnityEngine;

namespace DefaultNamespace
{
    public class P4MultiPlayerWindow : EditorWindow
    {
        const string k_PrefsKeyPrefix = "P4Debug";

        [MenuItem("Multiplayer/Show P4 Tools")]
        public static void ShowWindow()
        {
            GetWindow<P4MultiPlayerWindow>(false, "P4 Tools", true);
        }

        private void OnGUI()
        {
            EditorBool("Auto-start client and server", "AutoStart");
        }

        private static string GetKey(string subKey)
        {
            return k_PrefsKeyPrefix + "_" + Application.productName + "_" + subKey;
        }

        private static bool EditorBool(string label, string key = null)
        {
            var prefsKey = (string.IsNullOrEmpty(key) ? GetKey(label) : GetKey(key));
            var value    = EditorPrefs.GetBool(prefsKey);
            value = EditorGUILayout.Toggle(label, value);
            EditorPrefs.SetBool(prefsKey, value);
            return value;
        }
    }
}