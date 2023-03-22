using System;
using UnityEngine;

namespace ProceduralNoise
{
    public static class Voronoi
    {
        public static void Generate(RenderTexture texture, Parameters parameters)
        {
            throw new NotImplementedException();
        }

        [Serializable]
        public class Parameters : IParameters
        {
            public ChannelSettings redSettings = new(WriteType.Write);
            public ChannelSettings greenSettings = new(WriteType.Write);
            public ChannelSettings blueSettings = new(WriteType.Write);
            public ChannelSettings alphaSettings = new(WriteType.Write);
            
            public ChannelSettings RedSettings => redSettings;
            public ChannelSettings GreenSettings => greenSettings;
            public ChannelSettings BlueSettings => blueSettings;
            public ChannelSettings AlphaSettings => alphaSettings;
        }
    }
}
