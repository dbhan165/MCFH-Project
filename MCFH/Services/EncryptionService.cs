using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace MCFH.Services;

public class EncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["Encryption:Key"];
        if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 32)
        {
            throw new InvalidOperationException("Encryption:Key is missing or too short. It must be at least 32 characters.");
        }
        
        // Dùng 32 byte đầu tiên để chạy AES-256
        _key = Encoding.UTF8.GetBytes(keyString.Substring(0, 32));
    }

    public string? Encrypt(string? plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        
        // Lưu IV vào phần đầu của stream để sau này lấy ra decrypt
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        try
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;

            // Đọc 16 byte đầu tiên để lấy IV
            var iv = new byte[aes.BlockSize / 8];
            if (fullCipher.Length < iv.Length) return cipherText; // Không đúng format

            Array.Copy(fullCipher, iv, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(fullCipher, iv.Length, fullCipher.Length - iv.Length);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
        catch
        {
            // Nếu có lỗi giải mã (do đổi key hoặc data cũ không mã hóa), có thể trả về nguyên bản
            return cipherText;
        }
    }
}
