using Assets.Scripts.Views;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor
{
    [CustomEditor(typeof(CardSpriteLibrary))]
    public sealed class CardSpriteLibraryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            var library = (CardSpriteLibrary)target;

            if (GUILayout.Button("Auto Fill From Sprite Names", GUILayout.Height(30)))
            {
                library.AutoFillFromFolder();
            }
        }
    }
}