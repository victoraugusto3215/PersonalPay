using System.Threading.Channels;

namespace PersonalPay.Web.Services;

/// <summary>
/// Fila em memória entre o endpoint de webhook (que só valida e enfileira,
/// pra responder rápido ao Mercado Pago) e o worker que de fato processa o
/// pagamento. Sem Redis/RabbitMQ nesta versão — decouplar webhook↔processamento
/// já prova o conceito sem infraestrutura extra pra hospedar.
/// </summary>
public class CobrancaQueue
{
    private readonly Channel<long> _channel = Channel.CreateUnbounded<long>();

    public ValueTask EnfileirarAsync(long paymentId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(paymentId, ct);

    public IAsyncEnumerable<long> LerAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
