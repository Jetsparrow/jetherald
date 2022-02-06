using System.Security.Cryptography;

namespace JetHerald;
public static class TokenHelper
{
    static readonly byte[] buf = new byte[24];
    static readonly object SyncLock = new();

    public static string GetToken(int length = 32)
    {
        var byteLength = (length + 3) / 4 * 3;
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        var str = Convert.ToBase64String(bytes).Substring(0, length);
        return str.Replace('+', '_').Replace('/', '_');
    }
}
