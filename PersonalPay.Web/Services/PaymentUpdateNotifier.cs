namespace PersonalPay.Web.Services;

/// <summary>
/// Blazor Server já mantém uma conexão viva por usuário (é SignalR por baixo
/// dos panos) — em vez de montar um Hub próprio redundante, o worker chama
/// <see cref="Notificar"/> e os componentes assinam <see cref="Alterado"/>
/// pra saber quando recarregar e chamar StateHasChanged().
/// </summary>
public class PaymentUpdateNotifier
{
    public event Action? Alterado;

    public void Notificar() => Alterado?.Invoke();
}
