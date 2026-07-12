using Microsoft.EntityFrameworkCore;
using PersonalPay.Web.Components;
using PersonalPay.Web.Data;
using PersonalPay.Web.Models;
using PersonalPay.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// DbContextFactory (não AddDbContext) — o circuito do Blazor Server vive muito
// mais tempo que uma request HTTP normal; cada componente cria um DbContext
// de vida curta por operação em vez de compartilhar um escopo entre renders.
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

MercadoPago.Config.MercadoPagoConfig.AccessToken = builder.Configuration["MercadoPago:AccessToken"];
builder.Services.AddSingleton<MercadoPagoService>();

builder.Services.AddSingleton<CobrancaQueue>();
builder.Services.AddSingleton<PaymentUpdateNotifier>();
builder.Services.AddHostedService<CobrancaProcessingWorker>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<WhatsAppService>();
builder.Services.AddHostedService<VencimentoCheckerWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/webhooks/mercadopago", async (
    HttpRequest request,
    IDbContextFactory<AppDbContext> dbFactory,
    CobrancaQueue queue,
    IConfiguration config,
    ILogger<Program> logger) =>
{
    var type = request.Query["type"].FirstOrDefault();
    var dataId = request.Query["data.id"].FirstOrDefault();

    // O Mercado Pago também notifica outros tipos de evento (ex.: merchant_order) — só nos importa "payment".
    if (type != "payment" || string.IsNullOrWhiteSpace(dataId))
        return Results.Ok();

    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    var webhookSecret = config["MercadoPago:WebhookSecret"];
    if (!string.IsNullOrWhiteSpace(webhookSecret))
    {
        var signatureHeader = request.Headers["x-signature"].FirstOrDefault();
        var requestId = request.Headers["x-request-id"].FirstOrDefault();
        if (!MercadoPagoWebhookValidator.AssinaturaValida(signatureHeader, requestId, dataId, webhookSecret))
        {
            logger.LogWarning("Webhook do Mercado Pago com assinatura inválida — descartado.");
            return Results.Unauthorized();
        }
    }
    else
    {
        logger.LogWarning("MercadoPago:WebhookSecret não configurado — validação de assinatura desativada.");
    }

    var notificacaoId = MercadoPagoWebhookValidator.ExtrairNotificacaoId(body) ?? $"{dataId}-{DateTime.UtcNow.Ticks}";

    await using var db = await dbFactory.CreateDbContextAsync();
    if (await db.WebhookEventos.AnyAsync(w => w.NotificacaoId == notificacaoId))
        return Results.Ok(); // já vimos essa notificação — o Mercado Pago reenvia se não responder rápido

    db.WebhookEventos.Add(new WebhookEvento { NotificacaoId = notificacaoId, PayloadJson = body });
    await db.SaveChangesAsync();

    if (long.TryParse(dataId, out var paymentId))
        await queue.EnfileirarAsync(paymentId);

    return Results.Ok();
});

app.Run();
