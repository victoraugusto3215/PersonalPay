using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PersonalPay.Web.Services;

/// <summary>
/// Valida o header x-signature que o Mercado Pago manda em cada webhook, e
/// extrai o Id de notificação do corpo (usado pra dedupe). Formato documentado
/// em https://www.mercadopago.com.br/developers/pt/docs/checkout-api/webhooks —
/// vale conferir contra a doc vigente se o Mercado Pago mudar o formato.
/// </summary>
public static class MercadoPagoWebhookValidator
{
    /// <summary>
    /// x-signature chega como "ts=1704908010,v1=618c85345248dd820d5fd456117...".
    /// O manifesto assinado é "id:{data.id};request-id:{x-request-id};ts:{ts};",
    /// com HMAC-SHA256 usando o segredo do webhook (painel do Mercado Pago).
    /// </summary>
    public static bool AssinaturaValida(string? signatureHeader, string? requestId, string dataId, string secret)
    {
        if (string.IsNullOrWhiteSpace(signatureHeader))
            return false;

        string? ts = null;
        string? v1 = null;
        foreach (var part in signatureHeader.Split(','))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            var key = kv[0].Trim();
            var value = kv[1].Trim();
            if (key == "ts") ts = value;
            else if (key == "v1") v1 = value;
        }

        if (ts is null || v1 is null)
            return false;

        var manifest = $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{ts};";
        var chaveBytes = Encoding.UTF8.GetBytes(secret);
        var manifestBytes = Encoding.UTF8.GetBytes(manifest);
        var hashCalculado = Convert.ToHexStringLower(HMACSHA256.HashData(chaveBytes, manifestBytes));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hashCalculado),
            Encoding.UTF8.GetBytes(v1));
    }

    /// <summary>Id numérico da notificação (campo "id" na raiz do payload), usado pra dedupe.</summary>
    public static string? ExtrairNotificacaoId(string payloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(payloadJson);
            if (doc.RootElement.TryGetProperty("id", out var idProp))
                return idProp.ToString();
        }
        catch (JsonException)
        {
            // payload vazio/inválido — quem chama trata isso gerando um id de fallback.
        }
        return null;
    }
}
