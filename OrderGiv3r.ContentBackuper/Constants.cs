using TL;

namespace OrderGiv3r.Application;

public class Constants
{
    public static class MimeTypes
    {
        public static readonly Dictionary<Storage_FileType, string> Photos = new Dictionary<Storage_FileType, string>()
        {
            { Storage_FileType.jpeg, "image/jpeg" },
            { Storage_FileType.png, "image/png" },
            { Storage_FileType.webp, "image/webp" },
        };

        public static readonly Dictionary<Storage_FileType, string> Videos = new Dictionary<Storage_FileType, string>()
        {
            { Storage_FileType.gif, "image/gif" },
            { Storage_FileType.mov, "video/quicktime" },
            { Storage_FileType.mp4, "video/mp4" },
        };
    }
}
