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
        
        bool success = WedgieFormat.TryReadBase64UInt32(encodedBytes, out uint actualValue);

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
        
        bool success = WedgieFormat.TryWriteBase64UInt32(buffer, input);

        Assert.True(success);
        Assert.Equal(expectedEncodedValue, Encoding.UTF8.GetString(buffer));
    }
}