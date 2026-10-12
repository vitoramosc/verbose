using System.Security.Cryptography;
using System.Text;

namespace Verbose.Application.CasosDeUso;

public static class ProtecaoDoTokenDeConfirmacao
{
    public static string CriarToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static string GerarHash(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
