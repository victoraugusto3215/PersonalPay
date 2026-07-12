using Microsoft.EntityFrameworkCore;
using PersonalPay.Web.Data;
using PersonalPay.Web.Models;

namespace PersonalPay.Web.Services;

/// <summary>
/// Verifica periodicamente cobranças pendentes com vencimento no passado,
/// marca como vencidas e dispara o lembrete de WhatsApp — uma vez só por
/// cobrança (ver Cobranca.LembreteEnviado).
/// </summary>
public class VencimentoCheckerWorker(
    IDbContextFactory<AppDbContext> dbFactory,
    WhatsAppService whatsApp,
    PaymentUpdateNotifier notifier,
    IConfiguration config,
    ILogger<VencimentoCheckerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutos = config.GetValue("VencimentoChecker:IntervaloMinutos", 60);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutos));

        do
        {
            try
            {
                await VerificarAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha verificando cobranças vencidas.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task VerificarAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var hoje = DateOnly.FromDateTime(DateTime.Today);

        var vencidas = await db.Cobrancas
            .Include(c => c.Cliente)
            .Where(c => c.Status == StatusCobranca.Pendente && c.Vencimento < hoje)
            .ToListAsync(ct);

        if (vencidas.Count == 0)
            return;

        foreach (var cobranca in vencidas)
            cobranca.Status = StatusCobranca.Vencida;

        await db.SaveChangesAsync(ct);
        notifier.Notificar();

        foreach (var cobranca in vencidas.Where(c => !c.LembreteEnviado && c.Cliente is not null))
        {
            var tentou = await whatsApp.EnviarLembreteVencimentoAsync(cobranca.Cliente!, cobranca);
            if (tentou)
                cobranca.LembreteEnviado = true;
        }

        await db.SaveChangesAsync(ct);
    }
}
