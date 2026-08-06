using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// AES 加密 / CRC32 校验 静态工具类
/// 文件格式: [4 bytes CRC32][16 bytes IV][剩余 cipher data]
/// </summary>
public static class CryptoHelper
{
    private static readonly string EncryptionKey = "A1B2C3D4E5F6G7H8";

    // CRC32 查找表 (多项式 0xEDB88320)
    private static readonly uint[] Crc32Table = new uint[256];

    static CryptoHelper()
    {
        const uint polynomial = 0xEDB88320u;
        for (uint i = 0; i < 256; i++)
        {
            uint crc = i;
            for (int j = 0; j < 8; j++)
            {
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }
            Crc32Table[i] = crc;
        }
    }

    /// <summary>
    /// 生成随机 16 字节 IV
    /// </summary>
    public static byte[] GenerateRandomIV()
    {
        byte[] iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }
        return iv;
    }

    /// <summary>
    /// AES 加密，返回 (密文, IV)
    /// </summary>
    public static (byte[] cipher, byte[] iv) Encrypt(string plainText)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);
        byte[] iv = GenerateRandomIV();
        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

        using (Aes aes = Aes.Create())
        {
            aes.Key = keyBytes;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(plainBytes, 0, plainBytes.Length);
                    cs.FlushFinalBlock();
                }
                return (ms.ToArray(), iv);
            }
        }
    }

    /// <summary>
    /// AES 解密，失败返回 null
    /// </summary>
    public static string Decrypt(byte[] cipher, byte[] iv)
    {
        if (cipher == null || iv == null)
            return null;

        try
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(EncryptionKey);

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream(cipher))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs, Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 计算 CRC32 校验值
    /// </summary>
    public static uint ComputeCRC32(byte[] data)
    {
        if (data == null)
            return 0;

        uint crc = 0xFFFFFFFFu;
        for (int i = 0; i < data.Length; i++)
        {
            byte index = (byte)((crc ^ data[i]) & 0xFF);
            crc = (crc >> 8) ^ Crc32Table[index];
        }
        return crc ^ 0xFFFFFFFFu;
    }

    /// <summary>
    /// 将字节数组写入加密格式: [4 bytes CRC32][16 bytes IV][cipher data]
    /// </summary>
    public static byte[] PackEncryptedData(byte[] cipher, byte[] iv)
    {
        if (cipher == null || iv == null)
            return null;

        // 计算 cipher+iv 组合数据的 CRC32
        byte[] combined = new byte[iv.Length + cipher.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(cipher, 0, combined, iv.Length, cipher.Length);

        uint crc = ComputeCRC32(combined);
        byte[] crcBytes = BitConverter.GetBytes(crc);

        byte[] result = new byte[4 + iv.Length + cipher.Length];
        Buffer.BlockCopy(crcBytes, 0, result, 0, 4);
        Buffer.BlockCopy(iv, 0, result, 4, iv.Length);
        Buffer.BlockCopy(cipher, 0, result, 4 + iv.Length, cipher.Length);

        return result;
    }

    /// <summary>
    /// 从加密格式解包，验证 CRC32，返回 (密文, IV)；校验失败返回 (null, null)
    /// </summary>
    public static (byte[] cipher, byte[] iv) UnpackAndVerify(byte[] packedData)
    {
        if (packedData == null || packedData.Length < 4 + 16 + 1)
            return (null, null);

        // 读取 CRC32
        uint storedCrc = BitConverter.ToUInt32(packedData, 0);

        // 提取 IV 和 cipher
        byte[] iv = new byte[16];
        Buffer.BlockCopy(packedData, 4, iv, 0, 16);

        int cipherLength = packedData.Length - 4 - 16;
        byte[] cipher = new byte[cipherLength];
        Buffer.BlockCopy(packedData, 4 + 16, cipher, 0, cipherLength);

        // 验证 CRC32
        byte[] combined = new byte[iv.Length + cipher.Length];
        Buffer.BlockCopy(iv, 0, combined, 0, iv.Length);
        Buffer.BlockCopy(cipher, 0, combined, iv.Length, cipher.Length);

        uint computedCrc = ComputeCRC32(combined);
        if (computedCrc != storedCrc)
            return (null, null);

        return (cipher, iv);
    }
}
