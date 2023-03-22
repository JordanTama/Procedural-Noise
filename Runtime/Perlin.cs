using System;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace ProceduralNoise
{
    public static class Perlin
    {
        public static void Generate(RenderTexture texture, Parameters parameters)
        {
            bool is3D = texture.dimension == TextureDimension.Tex3D;
            int kernelIndex = is3D ? 1 : 0;
            string suffix = is3D ? "_3d" : "_2d";
            
            // Set target texture
            Settings.PerlinShader.SetTexture(kernelIndex, "result" + suffix, texture);

            // Set FBM parameters
            Settings.PerlinShader.SetInt("octaves", parameters.octaves);
            Settings.PerlinShader.SetFloat("lacunarity", parameters.lacunarity);
            Settings.PerlinShader.SetFloat("persistence", parameters.persistence);
            
            // Set gradient vectors
            int gradientCount = parameters.cellCount.x * parameters.cellCount.y * parameters.cellCount.z;
            if (is3D)
                gradientCount *= parameters.cellCount.z;
            ComputeBuffer buffer = new(gradientCount, sizeof(float) * (is3D ? 3 : 2));
            int seed = Random.Range(int.MinValue, int.MaxValue);
            buffer.SetData(is3D ? GenerateGradients3D(gradientCount, seed) : GenerateGradients2D(gradientCount, seed));
            Settings.PerlinShader.SetBuffer(kernelIndex, "gradients" + suffix, buffer);
            Settings.PerlinShader.SetInts("cell_count", parameters.cellCount.x, parameters.cellCount.y,
                parameters.cellCount.z);
            
            // Set region parameters
            Vector4 min = new(parameters.region.min.x, parameters.region.min.y, parameters.region.min.z, 0);
            Vector4 max = new(parameters.region.max.x, parameters.region.max.y, parameters.region.max.z, 0);
            Vector4 column1 = new(min.x, max.x, 0, 0);
            Vector4 column2 = new(min.y, max.y, 0, 0);
            Vector4 column3 = new(min.z, max.z, 0, 0);
            Vector4 column4 = new(0, 0, 0, 0);
            Settings.PerlinShader.SetMatrix("region", new Matrix4x4(column1, column2, column3, column4));
            
            // Set channel parameters
            Settings.PerlinShader.SetInts("write_types",
                (int)parameters.redSettings.writeType,
                (int)parameters.greenSettings.writeType,
                (int)parameters.blueSettings.writeType,
                (int)parameters.alphaSettings.writeType
            );
            
            // Set modifier parameters
            Settings.PerlinShader.SetBool("invert", parameters.invert);

            Vector3Int threadGroups = GetThreadGroups(texture);
            Settings.PerlinShader.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, threadGroups.z);
                
            buffer.Release();
        }

        private static Vector3Int GetThreadGroups(RenderTexture texture)
        {
            return new Vector3Int(
                Mathf.Max(Mathf.CeilToInt(texture.width / 8f), 1),
                Mathf.Max(Mathf.CeilToInt(texture.height / 8f), 1),
                Mathf.Max(Mathf.CeilToInt(texture.volumeDepth / 8f), 1)
            );
        }

        private static Vector2[] GenerateGradients2D(int count, int seed)
        {
            Random.InitState(seed);
            
            Vector2[] gradients = new Vector2[count];
            for (int i = 0; i < count; i++)
                gradients[i] = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

            return gradients;
        }
        
        private static Vector3[] GenerateGradients3D(int count, int seed)
        {
            Random.InitState(seed);
            
            Vector3[] gradients = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                gradients[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))
                    .normalized;
            }

            return gradients;
        }

        [Serializable]
        public class Parameters : IParameters
        {
            public Vector3Int cellCount = new(5, 5, 1);
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
