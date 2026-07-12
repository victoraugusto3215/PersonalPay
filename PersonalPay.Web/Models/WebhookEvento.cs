namespace PersonalPay.Web.Models;

/// <summary>
/// Log bruto de cada notificação recebida do Mercado Pago. O mesmo evento pode
/// chegar mais de uma vez (reenvio do provedor) — <see cref="NotificacaoId"/> é
/// único, e essa unicidade é o que garante que o mesmo pagamento não é
/// processado duas vezes (idempotência).
/// </summary>
public class WebhookEvento
{
    public int Id { get; set; }

    public string NotificacaoId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime RecebidoEm { get; set; } = DateTime.UtcNow;
    public bool Processado { get; set; }
}
