using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace ProceduralNoise
{
    public static class Worley
    {
        public static void Generate(RenderTexture texture, Parameters parameters)
        {
            bool is3D = texture.dimension == TextureDimension.Tex3D;
            int kernelIndex = is3D ? 1 : 0;
            string suffix = is3D ? "_3d" : "_2d";
            
            // Set texture
            Settings.WorleyShader.SetTexture(kernelIndex, "result" + suffix, texture);
            
            // Cells generation
            int cellCount = parameters.cellCount.x * parameters.cellCount.y * parameters.cellCount.z;
            ComputeBuffer points = new(cellCount, sizeof(int) * 3);
            points.SetData(GeneratePoints(cellCount, Random.Range(int.MinValue, int.MaxValue)));

            Settings.WorleyShader.SetBuffer(kernelIndex, "points" + suffix, points);
            Settings.WorleyShader.SetInts("cell_count", parameters.cellCount.x, parameters.cellCount.y,
                parameters.cellCount.z);
            
            // Parameters
            Settings.WorleyShader.SetInt("octaves", parameters.octaves);
            Settings.WorleyShader.SetFloat("lacunarity", parameters.lacunarity);
            Settings.WorleyShader.SetFloat("persistence", parameters.persistence);
            
            Vector4 min = new(parameters.region.min.x, parameters.region.min.y, parameters.region.min.z, 0);
            Vector4 max = new(parameters.region.max.x, parameters.region.max.y, parameters.region.max.z, 0);
            Vector4 column1 = new(min.x, max.x, 0, 0);
            Vector4 column2 = new(min.y, max.y, 0, 0);
            Vector4 column3 = new(min.z, max.z, 0, 0);
            Vector4 column4 = new(0, 0, 0, 0);
            Settings.WorleyShader.SetMatrix("region", new Matrix4x4(column1, column2, column3, column4));
            
            Settings.WorleyShader.SetInts("write_types",
                (int)parameters.redSettings.writeType,
                (int)parameters.greenSettings.writeType,
                (int)parameters.blueSettings.writeType,
                (int)parameters.alphaSettings.writeType
            );
            Settings.WorleyShader.SetBool("invert", parameters.invert);

            // Dispatch
            Vector3Int threadGroups = GetThreadGroups(texture);
            Settings.WorleyShader.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, threadGroups.z);
            
            // Cleanup
            points.Release();
        }
        
        private static Vector3Int GetThreadGroups(RenderTexture texture)
        {
            return new Vector3Int(
                Mathf.Max(Mathf.CeilToInt(texture.width / 8.0f), 1),
                Mathf.Max(Mathf.CeilToInt(texture.height / 8.0f), 1),
                Mathf.Max(Mathf.CeilToInt(texture.volumeDepth / 8.0f), 1)
            );
        }

        private static Vector3[] GeneratePoints(int count, int seed)
        {
            Random.InitState(seed);
            
            Vector3[] points = new Vector3[count];
            for (int i = 0; i < count; i++)
                points[i] = new Vector3(Random.value, Random.value, Random.value);
            
            return points;
        }

        [Serializable]
        public class Parameters : IParameters
        {
            public Vector3Int cellCount = new(5, 5, 5);
            public int octaves = 3;
            public float lacunarity = 2;
            public float persistence = 0.25f;
            public Bounds region = new(new Vector3(0.5f, 0.5f, 0.5f), Vector3.one);
            public ChannelSettings redSettings = new(WriteType.Write);
            public ChannelSettings greenSettings = new(WriteType.Write);
            public ChannelSettings blueSettings = new(WriteType.Write);
            public ChannelSettings alphaSettings = new(WriteType.Write);
            public bool invert;
            
            public ChannelSettings RedSettings => redSettings;
            public ChannelSettings GreenSettings => greenSettings;
            public ChannelSettings BlueSettings => blueSettings;
            public ChannelSettings AlphaSettings => alphaSettings;
        }
    }
}