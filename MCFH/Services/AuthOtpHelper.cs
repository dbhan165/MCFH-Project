using System.Security.Cryptography;

namespace MCFH.Services;

public static class AuthOtpHelper
{
    public static string GenerateCode()
    {
        return RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
    }
}
