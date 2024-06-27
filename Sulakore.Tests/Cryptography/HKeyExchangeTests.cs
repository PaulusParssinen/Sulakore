using Sulakore.Cryptography;
using Sulakore.Network;
using Sulakore.Network.Buffers;
using Sulakore.Network.Formats;

using System.Globalization;
using System.Numerics;
using Xunit;

namespace Sulakore.Tests.Cryptography;

public class HKeyExchangeTests
{
    [Fact]
    public void HKeyExchange_DH_TwoWay()
    {
        var first = new HKeyExchange(BigInteger.Zero, BigInteger.Zero);
        var second = new HKeyExchange(BigInteger.Zero, BigInteger.Zero);

        BigInteger firstPrivateKey = first.PrivateExponent;
    }

    [Fact]
    public async void HKeyExchange_VerifyPrimes_256()
    {
        //string p = "6857ceb3fb089e4361cb81dda4d52f38a4b6990c41f4fdaa661fb041825f2eee5937771e2faef139d6edf235087c1090930afbedc42c18402a1b502780ff5a220b5617bd6e7f69cc907cef91c84b6f59f7223678f50ada22c7db4ce287bb8b95e5279eef27f2e9a8a759be7fa4e4e909022ab2d5974022459c90ffd39aecdf8abb6806ecf80bd4eec68600ff5a6422f995ca10a6e0dc4a9cd6858071442ef75338e9b96e10c456d37972ce0b66abc70f44c6fb132957cfe2b4f304d1b70e87b614101e0a16194170f00ca524978e38436b24a92ae7c522346fdf8c42c3abbe7aef4434a280d17c13fcfb648e54e858d6441d2b836d02e605d3f0cea0828636c8";
        //string g = "53dba1c55a37b3aa12745d93c036495a5faf32d7f1efcde919acb18b68a3b8b6d463126827fe8c8e839cac1c5da6f0456d9b922268db2f8d070cb80c71d5bc39e9c74249784a1124ff33407453d3779976e88f7dd78d8891e59429fc27905da7ec03735e92a21e72fa78df4a283c0cf58067b30bc5d45283ca95a38ce1e5f95837d90a04ba743967cb179be9b14c4beb5eb11aa0cba652b12d8189ed33e97fec619b8cfb12898d0273a97a454aabe85724f92f64eb24c530e5aef65da364238c96df66ee5c82dbfed4b0a87b70a9eb242728a3ebe912d950b052c130884437b80f5a363018e7f586fb0bdd36dbdd68debea556b9678d5db75147a00c9dad1fbd";
        //string p = "5a2cc859394faa28030fd9ad8fd45cea7f9aa33e6a14011bc678f3a54833969ac7f404887c445cc6f9c53a01d944925be35b13b3d3cd92a1e5dc997a441388bc1237f98fcfb5c7370ec381e201260b3f8379ec25987fc4f034f7091a582da3aca6cd54d705e11712531c7126625d0bcc2ea5834e1a28a0f167c15afb80e4708af6ac58aac89cad99b631c406defa3f5d9d570e208b33ff4e850b01fa7676a2112c73499edee281155f6495385edba31965627e42a42215f4c9b02d6f5a617548740909efb93699e16959e924bf405476758d37af5c7e8e5c31d98071f847a4e7d7aa14683b1e79072e96a5bd3ca1120995c8a95ef16899816c2fb809805eb21b";
        //string g = "a77646216667808b7f8ca5827437d6533fb554fc525fb654228da898fd381051fac3e51d62f719373c8e98c9422772cbe373351f7f893391b42717b88baa24835107d847a91efabb880b12b45c81c2b1e2d893d66f052d4d04e33d72363d65b93e275ab0ac8f70d7fe3928dd8aa1f4de7a41a639f653c036f9967b5d292566c1e4143cb17603e6d4b03c578cc223173b6e4fa968c441e6d42df1e10b2659970230c90f25144d4551ff4aed57ecb1bc1167322164efa245d5f68f10b013e709080662a8d45a91ffd7ebdd2cdbdc5f3eba43ecffe09a10bd939503a07ecbd6f5ac87d689ab70e8f5cf318e4b6166a517cdd3038d1a4743efeb49e96fb352e2a000";
        string p = "7c71ffb46fbc883f0c866af7017471575f21b8c79c4271ae91841e01c76e274584ab0a8f8a709be93b87fb7f3adb9ddb9f14ac9d63ad0f514d8a538b60380d7d9b1a68e1d063ebc6124a2521eb253b40a8f1132e5ddfd60d94b19691463515b9cc399bfa97788e53387af2da71adfa6f26da159c3a904cc1d21f9834e7ad7bf51ca416ae7e70af14ea6b60f957487b438638dea83a6d9813ea7cc040ef0af761f9c9f7aafead2f760e0d1c5fd60f3cd13e2197385b491b98b2ecc0c41be69d36ecebc85f7440f09ecf9d21435874bffeddbe4da03c430ee62a29e8804bcd8e15a282d1f1fad383449325938189c2e427d97a31f9c4b69e08f052d3bdde9c6021";
        string g = "89a3917ce1acf00d0eea02615a344a8265d8b595b1c0707dcde294c7b532eeb14250eaaf500e0c01bd27795388f7187dc5ee134fb3b363624fc7139cccb282e3865f69178f13fa3f6ae899a4d6349e15833ddd594c19d3e43c6c8bc4d15062e7844fd3016f55cbb12c4fb2d1f7c5adb0f52d483c70ae086f7c36554c0eb936e39a850c8a57b98a51174cd47ab51a30ad7f5f8e923228e6914b5a7651d1f3efa56ed0cdfe0ad4763ef9bf6654bdf8b78229ef2d9cd80fca1061e6d2db8e2347aa04cc89a6dc28ff9f91074c08b3422f886e9b82d0ec9e287eb15086415a001d752617d84fd0da5285737ca1985f8fdca41f6910331f6b507f0f7d561786ea4fdd";
        string modulus = "C5DFF029848CD5CF4A84ADEFB2DA6685704920D5EBE8850B82C419A97B95302DE3B8021F37719FEBD4B3516E04D1E4702E74C468C9FF4BBBB5DD44A1E3A08687EDBEF7C30A176F7C8C83226A77F7982F7442D884D8149E924C486F43035C07B9167EA998416919DA4116D5E0598C11BA1542B4160136F04135C06EDF80170245E73C0DAD63895F52DCED3735582C5852744C8EC40AF576F26A9C8DC5B64ED3DAD40EFAAC6A76A1F5C2A422A8A4691F8991356467BDA61E1D34D0F35531058C8F741E4661ACFCB15C806A996AC312A8D33BF45079B89E11787537B37364749B883BDBFDE51A1A55086CF16159F5DEBCC76342AC2EF6950DA0C70C5845C97DFD49";
        
        var b_mod = BigInteger.Parse(modulus, NumberStyles.HexNumber);

        var b_p = BigInteger.Parse(p, NumberStyles.HexNumber);
        var b_g = BigInteger.Parse(g, NumberStyles.HexNumber);

        int mod_size = b_mod.GetByteCount();
        int b_p_size = b_p.GetByteCount();
        int b_g_size = b_g.GetByteCount();

        var keyExchange = new HKeyExchange(65537, b_mod);
        keyExchange.VerifyDHPrimes(b_p, b_g);

        var pub = keyExchange.GetPublicKey().ToString("X2");
    }

    [Fact]
    public async Task HKeyExchange_Remote_Verify()
    {
        string modulus = "C5DFF029848CD5CF4A84ADEFB2DA6685704920D5EBE8850B82C419A97B95302DE3B8021F37719FEBD4B3516E04D1E4702E74C468C9FF4BBBB5DD44A1E3A08687EDBEF7C30A176F7C8C83226A77F7982F7442D884D8149E924C486F43035C07B9167EA998416919DA4116D5E0598C11BA1542B4160136F04135C06EDF80170245E73C0DAD63895F52DCED3735582C5852744C8EC40AF576F26A9C8DC5B64ED3DAD40EFAAC6A76A1F5C2A422A8A4691F8991356467BDA61E1D34D0F35531058C8F741E4661ACFCB15C806A996AC312A8D33BF45079B89E11787537B37364749B883BDBFDE51A1A55086CF16159F5DEBCC76342AC2EF6950DA0C70C5845C97DFD49";
        
        BigInteger mod = BigInteger.Parse(modulus, NumberStyles.HexNumber);
        BigInteger exponent = new(65537);

        var node = await HNode.ConnectAsync("game-s2.habbo.com", 30000);
        node.ReceiveFormat = IHFormat.EvaWire;

        byte[] buffer = new byte[256];
        string version = "WIN63-202308031156-642235999";
        string type = "FLASH15";
        int len = 2 + 2 + version.Length + 2 + type.Length + 4 + 4;
        IHFormat.EvaWire.TryWrite(buffer, $"{len}{(ushort)4000}{version}{type}{6}{4}", out int written);

        await node.SendPacketAsync(buffer[..written]);

        // InitDiffie
        IHFormat.EvaWire.TryWrite(buffer, $"{2}{(ushort)878}", out written);
        await node.SendPacketAsync(buffer[..written]);

        var receiveBuffer = new byte[2048];
        int received = await node.ReceiveAsync(receiveBuffer);

        var (p2, g2) = ReadPG(receiveBuffer.AsSpan(0, received));

        var keyExchange = new HKeyExchange(exponent, mod);
        keyExchange.VerifyDHPrimes(p2, g2);

        // CompleteDiffie
        using var writer = new HPacketWriter(IHFormat.EvaWire, 1306);
        writer.WriteUTF8(keyExchange.GetPublicKey().ToString("X2"));

        received = await node.ReceiveAsync(receiveBuffer);

        static (BigInteger P, BigInteger G) ReadPG(Span<byte> buffer)
        {
            var reader = new HPacketReader(IHFormat.EvaWire, buffer);
            string p_h = reader.ReadUTF8();
            string g_h = reader.ReadUTF8();
            return (
                BigInteger.Parse("0" + p_h, NumberStyles.HexNumber),
                BigInteger.Parse("0" + g_h, NumberStyles.HexNumber));
        }
    }
}
