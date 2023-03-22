using System;

namespace ProceduralNoise
{
    [Serializable]
    public struct ChannelSettings
    {
        public WriteType writeType;

        public ChannelSettings(WriteType writeType)
        {
            this.writeType = writeType;
        }
    }
    
    [Serializable]
    public enum WriteType
    {
        Keep,
        Black,
        Grey,
        White,
        Write
    }
}
