using UnityEditor;
using UnityEngine;

namespace ProceduralNoise.Editor
{
    [CustomPropertyDrawer(typeof(Perlin.Parameters))]
    public class PerlinDrawer : PropertyDrawer
    {
        private bool _channelDropdown;
        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            SerializedProperty cellCount = property.FindPropertyRelative("cellCount");
            EditorGUILayout.PropertyField(cellCount);
            cellCount.vector3IntValue = Vector3Int.Max(cellCount.vector3IntValue, Vector3Int.one);

            SerializedProperty octaves = property.FindPropertyRelative("octaves");
            octaves.intValue = Mathf.Max(1, EditorGUILayout.IntSlider(octaves.displayName, octaves.intValue, 1, 10));

            SerializedProperty lacunarity = property.FindPropertyRelative("lacunarity");
            lacunarity.floatValue = Mathf.Max(0, EditorGUILayout.FloatField(lacunarity.displayName, lacunarity.floatValue));

            SerializedProperty persistence = property.FindPropertyRelative("persistence");
            persistence.floatValue = EditorGUILayout.Slider(persistence.displayName, persistence.floatValue, 0.0f, 1.0f);

            SerializedProperty region = property.FindPropertyRelative("region");
            EditorGUILayout.PropertyField(region);

            if (_channelDropdown = EditorGUILayout.Foldout(_channelDropdown, "Channel Settings"))
            {

                SerializedProperty red = property.FindPropertyRelative("redSettings");
                EditorGUILayout.PropertyField(red);

                SerializedProperty green = property.FindPropertyRelative("greenSettings");
                EditorGUILayout.PropertyField(green);

                SerializedProperty blue = property.FindPropertyRelative("blueSettings");
                EditorGUILayout.PropertyField(blue);

                SerializedProperty alpha = property.FindPropertyRelative("alphaSettings");
                EditorGUILayout.PropertyField(alpha);
            }

            SerializedProperty invert = property.FindPropertyRelative("invert");
            EditorGUILayout.PropertyField(invert);
        }
    }
}