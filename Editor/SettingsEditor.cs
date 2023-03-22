using System;
using UnityEditor;
using UnityEngine;

namespace ProceduralNoise.Editor
{
    [CustomEditor(typeof(Settings))]
    public class SettingsEditor : UnityEditor.Editor
    {
        public NoiseType SelectedNoise { get; private set; } = NoiseType.Perlin;

        private SerializedProperty _perlin;
        private SerializedProperty _voronoi;
        private SerializedProperty _worley;

        private string[] _toolbarNames;

        private void OnEnable()
        {
            _perlin = serializedObject.FindProperty("perlinParameters");
            _voronoi = serializedObject.FindProperty("voronoiParameters");
            _worley = serializedObject.FindProperty("worleyParameters");

            _toolbarNames = Enum.GetNames(typeof(NoiseType));
        }

        public override void OnInspectorGUI()
        {
            SelectedNoise = (NoiseType) GUILayout.Toolbar((int) SelectedNoise, _toolbarNames);

            switch (SelectedNoise)
            {
                case NoiseType.Perlin:
                    EditorGUILayout.PropertyField(_perlin);
                    break;
                
                case NoiseType.Voronoi:
                    EditorGUILayout.PropertyField(_voronoi);
                    break;
                
                case NoiseType.Worley:
                    EditorGUILayout.PropertyField(_worley);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        public enum NoiseType
        {
            Perlin,
            Voronoi,
            Worley
        }
    }
}