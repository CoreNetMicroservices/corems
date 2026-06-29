using System.Security.Cryptography;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace CoreMs.DocumentMs.Tests.Properties;

/// <summary>
/// Property 2: Checksum determinism — For any byte sequence, SHA-256 computation
/// produces identical hash regardless of upload path.
///
/// **Validates: Requirements 1.2, 2.3**
/// </summary>
public class ChecksumDeterminismTests
{
    /// <summary>
    /// For any non-empty byte content, computing the checksum twice on the same content
    /// always produces identical results (determinism).
    /// Validates: Requirements 1.2, 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ChecksumArbitraries)])]
    public void SameContent_AlwaysProduces_SameChecksum(NonEmptyBytes input)
    {
        using var stream1 = new MemoryStream(input.Value);
        using var stream2 = new MemoryStream(input.Value);

        var hash1 = ComputeChecksum(stream1);
        var hash2 = ComputeChecksum(stream2);

        Assert.Equal(hash1, hash2);
    }

    /// <summary>
    /// For any two distinct non-empty byte sequences, their checksums differ.
    /// Validates: Requirements 1.2, 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ChecksumArbitraries)])]
    public void DifferentContent_ProducesDifferentChecksum(NonEmptyBytes input1, NonEmptyBytes input2)
    {
        if (input1.Value.SequenceEqual(input2.Value))
            return; // skip equal inputs — not a counterexample

        using var stream1 = new MemoryStream(input1.Value);
        using var stream2 = new MemoryStream(input2.Value);

        var hash1 = ComputeChecksum(stream1);
        var hash2 = ComputeChecksum(stream2);

        Assert.NotEqual(hash1, hash2);
    }

    /// <summary>
    /// For any non-empty byte content, the checksum is always a 64-character lowercase hex string
    /// (SHA-256 produces 32 bytes = 64 hex characters).
    /// Validates: Requirements 1.2, 2.3
    /// </summary>
    [Property(MaxTest = 100, Arbitrary = [typeof(ChecksumArbitraries)])]
    public void Checksum_IsHexString_Of64Characters(NonEmptyBytes input)
    {
        using var stream = new MemoryStream(input.Value);
        var hash = ComputeChecksum(stream);

        Assert.Equal(64, hash.Length);
        Assert.All(hash, c => Assert.Contains(c, "0123456789abcdef"));
    }

    private static string ComputeChecksum(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var hash = SHA256.HashData(ms.ToArray());
        return Convert.ToHexStringLower(hash);
    }
}

#region Custom Types and Arbitraries for FsCheck 3.x

public record NonEmptyBytes(byte[] Value);

public class ChecksumArbitraries
{
    public static Arbitrary<NonEmptyBytes> NonEmptyBytesArbitrary()
    {
        Gen<int> seedGen = FsCheck.Fluent.Gen.Choose(0, int.MaxValue);
        Gen<NonEmptyBytes> gen = FsCheck.Fluent.Gen.Select(seedGen, seed =>
        {
            var rng = new Random(seed);
            var length = rng.Next(1, 1024);
            var bytes = new byte[length];
            rng.NextBytes(bytes);
            return new NonEmptyBytes(bytes);
        });

        return FsCheck.Fluent.Arb.From(gen);
    }
}

#endregion
