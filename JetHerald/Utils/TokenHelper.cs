using System.Security.Cryptography;

namespace JetHerald;
public static class TokenHelper
{
    static readonly byte[] buf = new byte[24];
    static readonly object SyncLock = new();

    public static string GetToken()
    {
        lock (SyncLock)
        {
            RandomNumberGenerator.Fill(buf);
            return Convert.ToBase64String(buf).Replace('+', '_').Replace('/', '_');
        }
    }
}
