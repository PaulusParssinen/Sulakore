using System.Text;
using Sulakore.Network.Formats;
using Xunit;

namespace Sulakore.Tests.Formats;

public class WedgieFormatEncodingTests
{
    [Theory]
    [InlineData("@", 0)]
    [InlineData("A", 1)]
    [InlineData("@@B", 2)]
    [InlineData("CJ", 202)]
    [InlineData("Ty", 1337)]
    public void TryReadBase64UInt32_SingleValue_ShouldReadValid(string encoded, uint expectedValue)
    {
        var encodedBytes = Encoding.UTF8.GetBytes(encoded);

        bool success = WedgieOutFormat.TryReadBase64UInt32(encodedBytes, out uint actualValue);

        Assert.True(success);
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [InlineData(0, "@")]
    [InlineData(1, "A")]
    [InlineData(2, "@@B")]
    [InlineData(202, "CJ")]
    [InlineData(1337, "Ty")]
    public void TryWriteBase64UInt32_SingleValue_ShouldReadValid(uint input, string expectedEncodedValue)
    {
        Span<byte> buffer = stackalloc byte[expectedEncodedValue.Length];

        bool success = WedgieOutFormat.TryWriteBase64UInt32(buffer, input);

        Assert.True(success);
        Assert.Equal(expectedEncodedValue, Encoding.UTF8.GetString(buffer));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(-1, 1)]
    [InlineData(2, 1)]
    [InlineData(-2, 1)]
    [InlineData(3, 1)]
    [InlineData(-3, 1)]
    [InlineData(4, 2)]
    [InlineData(-4, 2)]
    [InlineData(128, 2)]
    [InlineData(-128, 2)]
    [InlineData(255, 2)]
    [InlineData(-255, 2)]
    [InlineData(256, 3)]
    [InlineData(-256, 3)]
    [InlineData(8192, 3)]
    [InlineData(-8192, 3)]
    [InlineData(16383, 3)]
    [InlineData(-16383, 3)]
    [InlineData(16384, 4)]
    [InlineData(-16384, 4)]
    [InlineData(1048575, 4)]
    [InlineData(-1048575, 4)]
    [InlineData(1048576, 5)]
    [InlineData(-1048576, 5)]
    public void GetEncodedVL64Length_IsCorrect(int value, int expectedEncodedLength)
    {
        Assert.Equal(expectedEncodedLength, WedgieOutFormat.GetEncodedVL64Int32Length(value));
    }
}