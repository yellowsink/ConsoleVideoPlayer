using System.IO;
using MessagePack;
using MessagePack.Resolvers;

namespace ConsoleVideoPlayer.Player
{
    // ReSharper disable once InconsistentNaming
    public static class FrameIO
    {
        public static void Save(this SavedFrames frames, string savePath)
        {
            var msgpack = MessagePackSerializer.Serialize(frames, ContractlessStandardResolver.Options);
            File.WriteAllBytes(savePath, msgpack);
        }

        public static SavedFrames ReadFrames(string savePath)
        {
            var msgpack = File.ReadAllBytes(savePath);
            return MessagePackSerializer.Deserialize<SavedFrames>(msgpack);
        }
    }

    [MessagePackObject]
    public class SavedFrames
    {
        // ReSharper disable PropertyCanBeMadeInitOnly.Global
        [Key(0)] public string[] Frames    { get; set; }
        [Key(1)] public double   Framerate { get; set; }
        [Key(2)] public byte[]   Audio     { get; set; }
        // ReSharper restore PropertyCanBeMadeInitOnly.Global
    }
}