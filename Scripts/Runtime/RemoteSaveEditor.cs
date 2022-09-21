using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CodySource
{
    namespace RemoteSave
    {
#if UNITY_EDITOR
        [CustomEditor(typeof(RemoteSave), true)]
        public class RemoteSaveEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                if (GUILayout.Button("Generate PHP")) ((RemoteSave)target)._GeneratePHP();
                GUILayout.Space(10f);
                DrawDefaultInspector();
            }
        }
#else
        public class RemoteSaveEditor {}
#endif
    }
}