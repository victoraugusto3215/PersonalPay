namespace PersonalPay.Web.Models;

public enum StatusCobranca
{
    Pendente,
    Paga,
    Vencida,
    Cancelada,
}

public class Cobranca
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly Vencimento { get; set; }
    public StatusCobranca Status { get; set; } = StatusCobranca.Pendente;

    /// <summary>Id do pagamento no Mercado Pago — usado pra consultar status e casar com o webhook.</summary>
    public string? MercadoPagoPaymentId { get; set; }

    public string? PixCopiaECola { get; set; }
    public string? PixQrCodeBase64 { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? PagoEm { get; set; }

    /// <summary>Evita mandar mais de um lembrete de WhatsApp pra mesma cobrança vencida.</summary>
    public bool LembreteEnviado { get; set; }
}
