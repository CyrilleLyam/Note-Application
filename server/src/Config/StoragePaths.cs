using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace server.src.Config;

public static class StoragePaths
{
    public static string UserFolder(int userId) => $"{Shard(userId)}/{userId}";

    public static string Key(string folder, string fileName) => $"{folder}/{fileName}";

    public static (string Folder, string FileName) Split(string key)
    {
        var index = key.LastIndexOf('/');
        return (key[..index], key[(index + 1)..]);
    }

    private static string Shard(int userId)
    {
        var bytes = Encoding.UTF8.GetBytes(userId.ToString(CultureInfo.InvariantCulture));
        return Convert.ToHexStringLower(SHA256.HashData(bytes))[..2];
    }
}
