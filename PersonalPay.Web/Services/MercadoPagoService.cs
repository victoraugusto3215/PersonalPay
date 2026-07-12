using MercadoPago.Client.Payment;
using MercadoPago.Resource.Payment;
using PersonalPay.Web.Models;

namespace PersonalPay.Web.Services;

public record PixGerado(string PaymentId, string? QrCodeCopiaECola, string? QrCodeBase64);

public class MercadoPagoService(IConfiguration configuration)
{
    private readonly PaymentClient _paymentClient = new();

    private bool Configurado => !string.IsNullOrWhiteSpace(configuration["MercadoPago:AccessToken"]);

    public async Task<PixGerado> CriarCobrancaPixAsync(Cobranca cobranca, Cliente cliente)
    {
        if (!Configurado)
        {
            throw new InvalidOperationException(
                "Access Token do Mercado Pago não configurado. Rode: dotnet user-secrets set \"MercadoPago:AccessToken\" \"TEST-...\" dentro de PersonalPay.Web.");
        }

        var request = new PaymentCreateRequest
        {
            TransactionAmount = cobranca.Valor,
            Description = cobranca.Descricao,
            PaymentMethodId = "pix",
            ExternalReference = cobranca.Id.ToString(),
            Payer = new PaymentPayerRequest
            {
                Email = cliente.Email,
                FirstName = cliente.Nome,
            },
        };

        var requestOptions = new MercadoPago.Client.RequestOptions();
        requestOptions.CustomHeaders["X-Idempotency-Key"] = $"cobranca-{cobranca.Id}-{Guid.NewGuid()}";

        Payment payment = await _paymentClient.CreateAsync(request, requestOptions);

        var paymentId = payment.Id?.ToString()
            ?? throw new InvalidOperationException("Mercado Pago não retornou um Id de pagamento.");
        var transactionData = payment.PointOfInteraction?.TransactionData;

        return new PixGerado(paymentId, transactionData?.QrCode, transactionData?.QrCodeBase64);
    }

    /// <summary>Consulta o status atual de um pagamento — usado pelo worker ao processar o webhook.</summary>
    public async Task<string?> ConsultarStatusAsync(long paymentId)
    {
        var payment = await _paymentClient.GetAsync(paymentId);
        return payment.Status;
    }
}
