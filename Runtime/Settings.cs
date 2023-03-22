using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProceduralNoise
{
    public class Settings : ScriptableObject
    {
        [SerializeField] private ComputeShader perlinShader;
        [SerializeField] private ComputeShader worleyShader;
        [SerializeField] private Perlin.Parameters perlinParameters;
        [SerializeField] private Voronoi.Parameters voronoiParameters;
        [SerializeField] private Worley.Parameters worleyParameters;
        [SerializeField] private string path;
        [SerializeField] private string fileName;
        [SerializeField] private bool autoName;

        public static ComputeShader PerlinShader => Instance.perlinShader;
        public static ComputeShader WorleyShader => Instance.worleyShader;
        public static Perlin.Parameters PerlinParameters => Instance.perlinParameters;
        public static Voronoi.Parameters VoronoiParameters => Instance.voronoiParameters;
        public static Worley.Parameters WorleyParameters => Instance.worleyParameters;

        #if UNITY_EDITOR
        private void OnEnable()
        {
            perlinShader ??= AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Compute Shaders/Perlin.compute");
            worleyShader ??= AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Compute Shaders/Worley.compute");
        }
        #endif

        #region Singleton
        
        private const string InstanceDirectory = "ProceduralNoise/Cache/";
        private const string InstanceFileName = "Settings";

        private static Settings instance;

        public static Settings Instance
        {
            get
            {
                if (instance)
                    return instance;

                if (instance = Resources.Load<Settings>(InstanceDirectory + InstanceFileName))
                    return instance;
                
#if UNITY_EDITOR
                instance = CreateInstance<Settings>();
                Directory.CreateDirectory(Application.dataPath + "/Resources/" + InstanceDirectory);
                AssetDatabase.CreateAsset(instance,
                    "Assets/Resources/" + InstanceDirectory + InstanceFileName + ".asset");
#endif

                return instance;
            }
        }
        
        #endregion
    }
}
