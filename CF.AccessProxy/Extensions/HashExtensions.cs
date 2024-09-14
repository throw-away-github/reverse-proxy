using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IO;

namespace CF.AccessProxy.Extensions;

public static class HashExtensions
{
    private static readonly RecyclableMemoryStreamManager _manager = new();

    public static string ComputeHash(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[]? hashBuffer = null;

        try
        {
            hashBuffer = ArrayPool<byte>.Shared.Rent(SHA256.HashSizeInBytes);
            var hashBufferSpan = hashBuffer.AsSpan(0, SHA256.HashSizeInBytes);
 
            SHA256.HashData(stream, hashBufferSpan);
            return Convert.ToBase64String(hashBufferSpan);
        }
        finally
        {
            if (hashBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(hashBuffer);
            }
        }
    }

    public static string ComputeHash(this string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        
        using var stream = _manager.GetStream();
        byte[]? byteBuffer = null;

        try
        {
            var encoding = Encoding.UTF8;
            var maxByteCount = encoding.GetMaxByteCount(data.Length);

            byteBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);

            var count = encoding.GetBytes(data, byteBuffer);
            stream.Write(byteBuffer, 0, count);
            stream.Position = 0;

            ArrayPool<byte>.Shared.Return(byteBuffer);
            byteBuffer = null;

            return ComputeHash(stream);
        }
        finally
        {
            if (byteBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }
        }
    }

    public static string ComputeHash(this ReadOnlySequence<byte> data)
    {
        if (data.IsEmpty)
        {
            return string.Empty;
        }
        if (!data.IsSingleSegment)
        {
            using var stream = _manager.GetStream();
            foreach (var segment in data)
            {
                stream.Write(segment.Span);
            }
            return ComputeHash(stream);
        }

        byte[]? hashBuffer = null;

        try
        {
            hashBuffer = ArrayPool<byte>.Shared.Rent(SHA256.HashSizeInBytes);
            var hashBufferSpan = hashBuffer.AsSpan(0, SHA256.HashSizeInBytes);
 
            SHA256.HashData(data.FirstSpan, hashBufferSpan);
            return Convert.ToBase64String(hashBufferSpan);
        }
        finally
        {
            if (hashBuffer != null)
            {
                ArrayPool<byte>.Shared.Return(hashBuffer);
            }
        }
    }
}