using Microsoft.EntityFrameworkCore;
using PersonalPay.Web.Models;

namespace PersonalPay.Web.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Cobranca> Cobrancas => Set<Cobranca>();
    public DbSet<WebhookEvento> WebhookEventos => Set<WebhookEvento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cobranca>()
            .Property(c => c.Valor)
            .HasPrecision(10, 2);

        modelBuilder.Entity<WebhookEvento>()
            .HasIndex(w => w.NotificacaoId)
            .IsUnique();
    }
}
