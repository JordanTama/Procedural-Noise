namespace ProceduralNoise
{
    public interface IParameters
    {
        ChannelSettings RedSettings { get; }
        ChannelSettings GreenSettings { get; }
        ChannelSettings BlueSettings { get; }
        ChannelSettings AlphaSettings { get; }
    }
}
