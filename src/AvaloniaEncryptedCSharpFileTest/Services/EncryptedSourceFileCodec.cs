using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AvaloniaEncryptedCSharpFileTest.Services;

internal static class EncryptedSourceFileCodec
{
    private const byte Version = 1;
    private const int SaltSize = 16;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;
    private const int KeyIterations = 100_000;
    private static readonly byte[] Magic = "ACSF"u8.ToArray();
    private static readonly byte[] DemoSecret =
        Encoding.UTF8.GetBytes("AvaloniaEncryptedCSharpFileTest.SourceFileKey.v1");

    public static byte[] Encrypt(string sourceCode)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plainText = Encoding.UTF8.GetBytes(sourceCode);
        var cipherText = new byte[plainText.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(DeriveKey(salt), TagSize))
        {
            aes.Encrypt(nonce, plainText, cipherText, tag);
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(Version);
        WriteBlock(writer, salt);
        WriteBlock(writer, nonce);
        WriteBlock(writer, tag);
        WriteBlock(writer, cipherText);
        writer.Flush();
        return stream.ToArray();
    }

    public static string Decrypt(byte[] encryptedFile)
    {
        using var stream = new MemoryStream(encryptedFile);
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

        var magic = reader.ReadBytes(Magic.Length);
        if (!magic.AsSpan().SequenceEqual(Magic))
        {
            throw new InvalidDataException("不是有效的加密 C# 文件。");
        }

        var version = reader.ReadByte();
        if (version != Version)
        {
            throw new InvalidDataException($"不支持的加密 C# 文件版本：{version}。");
        }

        var salt = ReadBlock(reader, SaltSize);
        var nonce = ReadBlock(reader, NonceSize);
        var tag = ReadBlock(reader, TagSize);
        var cipherText = ReadBlock(reader);
        var plainText = new byte[cipherText.Length];

        using (var aes = new AesGcm(DeriveKey(salt), TagSize))
        {
            aes.Decrypt(nonce, cipherText, tag, plainText);
        }

        return Encoding.UTF8.GetString(plainText);
    }

    private static byte[] DeriveKey(byte[] salt)
    {
        return Rfc2898DeriveBytes.Pbkdf2(
            DemoSecret,
            salt,
            KeyIterations,
            HashAlgorithmName.SHA256,
            KeySize);
    }

    private static void WriteBlock(BinaryWriter writer, byte[] value)
    {
        writer.Write(value.Length);
        writer.Write(value);
    }

    private static byte[] ReadBlock(BinaryReader reader, int? expectedLength = null)
    {
        var length = reader.ReadInt32();
        if (length < 0)
        {
            throw new InvalidDataException("加密 C# 文件结构损坏。");
        }

        if (expectedLength is not null && length != expectedLength)
        {
            throw new InvalidDataException("加密 C# 文件结构损坏。");
        }

        var value = reader.ReadBytes(length);
        if (value.Length != length)
        {
            throw new InvalidDataException("加密 C# 文件内容不完整。");
        }

        return value;
    }
}
