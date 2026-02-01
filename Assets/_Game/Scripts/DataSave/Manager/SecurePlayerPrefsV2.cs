using System;
using System.Security.Cryptography;
using System.Text;
using Balduu.Utility;
using CodeStage.AntiCheat.Storage; // ObscuredPrefs
using UnityEngine;

public static class SecurePlayerPrefsV2
{
    // WARNING: Demo keys. Replace with keys from keystore/server in production.
    private static readonly byte[] AesKey  = Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF"); // 32B
    private static readonly byte[] HmacKey = Encoding.UTF8.GetBytes("HMAC_KEY_32_BYTES_EXAMPLE_HERE_1234"); // 32B

    // Debug switch
    public static bool UseEncryption { get; set; } = true;

    // v2 blob layout (Base64 inside ObscuredPrefs): [ver:1][ivLen:1][iv][cipher][tag:32]
    private const byte Version = 0x02;
    private const int TagLen = 32;

    // API tương đương PlayerPrefs
    public static void SetString(string key, string value)
    {
        DebugAds.Log(value);
        if (!UseEncryption)
        {
            ObscuredPrefs.Set(key, value);
            ObscuredPrefs.Save();
            return;
        }

        var iv = RandomBytes(16);
        var plain = Encoding.UTF8.GetBytes(value);
        var cipher = AesCbcEncrypt(plain, AesKey, iv);

        // AAD = key name để chống tráo dữ liệu giữa các key
        var aad = Encoding.UTF8.GetBytes(key);
        var macInput = Concat(new[] { Version, (byte)iv.Length }, Concat(iv, Concat(cipher, aad)));
        var tag = Hmac256(HmacKey, macInput);

        var blob = Concat(new[] { Version, (byte)iv.Length }, Concat(iv, Concat(cipher, tag)));
        var base64 = Convert.ToBase64String(blob);
        ObscuredPrefs.Set(key, base64);
        ObscuredPrefs.Save();
    }

    public static string GetString(string key, string defaultValue = "")
    {
        try
        {
            if (!ObscuredPrefs.HasKey(key)) return defaultValue;
            var base64 = ObscuredPrefs.Get(key, string.Empty);
            if (string.IsNullOrEmpty(base64)) return defaultValue;

            if (!UseEncryption) return base64;

            var raw = Convert.FromBase64String(base64);
            if (raw.Length < 2 + TagLen + 1) return defaultValue;

            int pos = 0;
            var ver = raw[pos++];
            if (ver != Version) return defaultValue; // không phải định dạng v2

            byte ivLen = raw[pos++];
            if (ivLen < 12 || ivLen > 16) return defaultValue;

            if (raw.Length < 2 + ivLen + TagLen + 1) return defaultValue;

            var iv = new byte[ivLen];
            Buffer.BlockCopy(raw, pos, iv, 0, ivLen);
            pos += ivLen;

            int cipherLen = raw.Length - pos - TagLen;
            if (cipherLen <= 0) return defaultValue;

            var cipher = new byte[cipherLen];
            Buffer.BlockCopy(raw, pos, cipher, 0, cipherLen);
            pos += cipherLen;

            var tag = new byte[TagLen];
            Buffer.BlockCopy(raw, pos, tag, 0, TagLen);

            var aad = Encoding.UTF8.GetBytes(key);
            var macInput = Concat(new[] { ver, ivLen }, Concat(iv, Concat(cipher, aad)));
            var expected = Hmac256(HmacKey, macInput);
            if (!FixedEquals(expected, tag)) return defaultValue;

            var plain = AesCbcDecrypt(cipher, AesKey, iv);
            return Encoding.UTF8.GetString(plain);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SecurePlayerPrefsV2 GetString error: {e.Message}");
            return defaultValue;
        }
    }

    public static bool HasKey(string key)
    {
        return ObscuredPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        ObscuredPrefs.DeleteKey(key);
    }

    public static void Save()
    {
        ObscuredPrefs.Save();
    }

    // -------- Migration helpers --------
    // Thử đọc theo v2; nếu không có, thử đọc từ SecurePlayerPrefs (legacy v1) rồi ghi sang v2.
    public static string GetStringWithLegacyFallback(string key, string defaultValue = "")
    {
        // 1) v2
        var v2 = GetString(key, null);
        if (v2 != null) return v2;

        return defaultValue;
    }

    // -------- crypto helpers --------
    private static byte[] AesCbcEncrypt(byte[] plain, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using (var enc = aes.CreateEncryptor())
            {
                return enc.TransformFinalBlock(plain, 0, plain.Length);
            }
        }
    }

    private static byte[] AesCbcDecrypt(byte[] cipher, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            using (var dec = aes.CreateDecryptor())
            {
                return dec.TransformFinalBlock(cipher, 0, cipher.Length);
            }
        }
    }

    private static byte[] Hmac256(byte[] key, byte[] data)
    {
        using (var h = new HMACSHA256(key))
            return h.ComputeHash(data);
    }

    private static byte[] RandomBytes(int len)
    {
        var b = new byte[len];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(b);
        return b;
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var r = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, r, 0, a.Length);
        Buffer.BlockCopy(b, 0, r, a.Length, b.Length);
        return r;
    }

    private static bool FixedEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}
