namespace PersonalPay.Web.Models;

public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;

    /// <summary>Formato E.164 (ex.: 5531989656108) — é o que a Cloud API do WhatsApp exige.</summary>
    public string WhatsApp { get; set; } = string.Empty;

    /// <summary>Exigido pelo Mercado Pago ao criar um pagamento (payer.email).</summary>
    public string Email { get; set; } = string.Empty;

    public List<Cobranca> Cobrancas { get; set; } = [];
}
