using Microsoft.EntityFrameworkCore;
using PersonalPay.Web.Data;
using PersonalPay.Web.Models;

namespace PersonalPay.Web.Services;

/// <summary>
/// Consome a fila alimentada pelo endpoint de webhook. Consulta o status real
/// do pagamento no Mercado Pago (a notificação só avisa "algo mudou", quem tem
/// o status de verdade é a API) e atualiza a cobrança correspondente.
/// </summary>
public class CobrancaProcessingWorker(
    CobrancaQueue queue,
    IDbContextFactory<AppDbContext> dbFactory,
    MercadoPagoService mercadoPago,
    PaymentUpdateNotifier notifier,
    ILogger<CobrancaProcessingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var paymentId in queue.LerAsync(stoppingToken))
        {
            try
            {
                await ProcessarAsync(paymentId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha processando o pagamento {PaymentId}", paymentId);
            }
        }
    }

    private async Task ProcessarAsync(long paymentId)
    {
        var status = await mercadoPago.ConsultarStatusAsync(paymentId);

        await using var db = await dbFactory.CreateDbContextAsync();
        var cobranca = await db.Cobrancas.FirstOrDefaultAsync(c => c.MercadoPagoPaymentId == paymentId.ToString());
        if (cobranca is null)
        {
            logger.LogWarning("Webhook recebido pro pagamento {PaymentId}, mas nenhuma cobrança bate com esse Id.", paymentId);
            return;
        }

        var novoStatus = status switch
        {
            "approved" => StatusCobranca.Paga,
            "cancelled" or "rejected" => StatusCobranca.Cancelada,
            _ => cobranca.Status,
        };

        // Idempotente por natureza: reaplicar o mesmo status não muda nada,
        // então reprocessar a mesma notificação (ou uma atrasada/fora de ordem) é seguro.
        if (novoStatus == cobranca.Status)
            return;

        cobranca.Status = novoStatus;
        if (novoStatus == StatusCobranca.Paga)
            cobranca.PagoEm = DateTime.UtcNow;

        await db.SaveChangesAsync();
        notifier.Notificar();
    }
}
