using PersonalPay.Web.Services;

namespace PersonalPay.Tests;

public class MercadoPagoWebhookValidatorTests
{
    // Vetor calculado independentemente (Node.js crypto.createHmac) para o
    // manifesto "id:12345;request-id:req-abc-123;ts:1704908010;" com o
    // segredo abaixo — não é um valor inventado, é HMAC-SHA256 real.
    private const string DataId = "12345";
    private const string RequestId = "req-abc-123";
    private const string Timestamp = "1704908010";
    private const string Secret = "minha-chave-secreta-teste";
    private const string ValidV1 = "2b6ef2454e4f6483af6251dad4bec6ec9ce53746217744d5d3a05d53d51f68ed";

    private static string BuildHeader(string ts, string v1) => $"ts={ts},v1={v1}";

    [Fact]
    public void AssinaturaValida_ComAssinaturaCorreta_RetornaTrue()
    {
        var header = BuildHeader(Timestamp, ValidV1);

        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId, Secret);

        Assert.True(resultado);
    }

    [Fact]
    public void AssinaturaValida_ComDataIdEmCaixaDiferente_AindaValidaPorqueManifestoUsaLowercase()
    {
        var header = BuildHeader(Timestamp, ValidV1);

        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId.ToUpperInvariant(), Secret);

        Assert.True(resultado);
    }

    [Fact]
    public void AssinaturaValida_ComV1Adulterado_RetornaFalse()
    {
        var header = BuildHeader(Timestamp, "0000000000000000000000000000000000000000000000000000000000000000");

        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId, Secret);

        Assert.False(resultado);
    }

    [Fact]
    public void AssinaturaValida_ComSegredoErrado_RetornaFalse()
    {
        var header = BuildHeader(Timestamp, ValidV1);

        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId, "segredo-errado");

        Assert.False(resultado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AssinaturaValida_ComHeaderVazioOuNulo_RetornaFalse(string? header)
    {
        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId, Secret);

        Assert.False(resultado);
    }

    [Fact]
    public void AssinaturaValida_ComHeaderFaltandoV1_RetornaFalse()
    {
        var header = $"ts={Timestamp}";

        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId, Secret);

        Assert.False(resultado);
    }

    [Fact]
    public void AssinaturaValida_ComHeaderFaltandoTs_RetornaFalse()
    {
        var header = $"v1={ValidV1}";

        var resultado = MercadoPagoWebhookValidator.AssinaturaValida(header, RequestId, DataId, Secret);

        Assert.False(resultado);
    }

    [Fact]
    public void ExtrairNotificacaoId_ComPayloadValido_RetornaId()
    {
        var payload = """{"id": 999888, "data": {"id": "12345"}}""";

        var resultado = MercadoPagoWebhookValidator.ExtrairNotificacaoId(payload);

        Assert.Equal("999888", resultado);
    }

    [Fact]
    public void ExtrairNotificacaoId_ComJsonInvalido_RetornaNull()
    {
        var resultado = MercadoPagoWebhookValidator.ExtrairNotificacaoId("isso não é json");

        Assert.Null(resultado);
    }

    [Fact]
    public void ExtrairNotificacaoId_SemCampoId_RetornaNull()
    {
        var payload = """{"data": {"id": "12345"}}""";

        var resultado = MercadoPagoWebhookValidator.ExtrairNotificacaoId(payload);

        Assert.Null(resultado);
    }
}
