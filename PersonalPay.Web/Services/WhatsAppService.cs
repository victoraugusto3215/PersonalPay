using System.Net.Http.Headers;
using System.Net.Http.Json;
using PersonalPay.Web.Models;

namespace PersonalPay.Web.Services;

/// <summary>
/// Envia o lembrete de cobrança vencida via Cloud API do WhatsApp (Meta).
/// Fora da janela de 24h de conversa iniciada pelo cliente, a Cloud API só
/// permite mensagem via template pré-aprovado — por isso isso manda um
/// template, não texto livre. O template precisa existir e estar aprovado
/// no painel da Meta antes de funcionar (ver README).
/// </summary>
public class WhatsAppService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<WhatsAppService> logger)
{
    private bool Configurado =>
        !string.IsNullOrWhiteSpace(config["WhatsApp:Token"]) && !string.IsNullOrWhiteSpace(config["WhatsApp:PhoneNumberId"]);

    /// <returns>
    /// true se o envio foi de fato tentado (independente da Meta ter aceitado),
    /// false se foi pulado por falta de configuração — nesse caso o worker deve
    /// tentar de novo no próximo ciclo em vez de marcar como enviado.
    /// </returns>
    public async Task<bool> EnviarLembreteVencimentoAsync(Cliente cliente, Cobranca cobranca)
    {
        if (!Configurado)
        {
            logger.LogWarning(
                "WhatsApp não configurado — lembrete da cobrança {CobrancaId} não foi enviado. " +
                "Rode: dotnet user-secrets set \"WhatsApp:Token\" \"...\" e \"WhatsApp:PhoneNumberId\" \"...\".",
                cobranca.Id);
            return false;
        }

        var phoneNumberId = config["WhatsApp:PhoneNumberId"];
        var templateName = config["WhatsApp:TemplateName"] ?? "cobranca_vencida";
        var languageCode = config["WhatsApp:LanguageCode"] ?? "pt_BR";

        var payload = new
        {
            messaging_product = "whatsapp",
            to = cliente.WhatsApp,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = cliente.Nome },
                            new { type = "text", text = cobranca.Valor.ToString("C") },
                            new { type = "text", text = cobranca.Vencimento.ToString("dd/MM/yyyy") },
                        },
                    },
                },
            },
        };

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config["WhatsApp:Token"]);

        var response = await client.PostAsJsonAsync($"https://graph.facebook.com/v21.0/{phoneNumberId}/messages", payload);
        if (!response.IsSuccessStatusCode)
        {
            var erro = await response.Content.ReadAsStringAsync();
            logger.LogError("Falha ao enviar WhatsApp pra cobrança {CobrancaId}: {Erro}", cobranca.Id, erro);
        }

        return true;
    }
}
