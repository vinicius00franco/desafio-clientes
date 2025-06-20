using Microsoft.EntityFrameworkCore;
using ApiBackend.Features.Clientes.Models;

namespace ApiBackend.Data;

public class ContextoApp : DbContext
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Endereco> Enderecos => Set<Endereco>();
    public DbSet<Contato> Contatos => Set<Contato>();

    public ContextoApp(DbContextOptions<ContextoApp> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasSequence<int>("seq_cliente");
        mb.HasSequence<int>("seq_endereco");
        mb.HasSequence<int>("seq_contato");

        mb.Entity<Cliente>(c =>
        {
            c.Property(p => p.ClienteId)
                .HasDefaultValueSql("NEXT VALUE FOR seq_cliente");
            c.HasMany(p => p.Enderecos)
                .WithOne(e => e.Cliente)
                .HasForeignKey(e => e.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
            c.HasMany(p => p.Contatos)
                .WithOne(ct => ct.Cliente)
                .HasForeignKey(ct => ct.ClienteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        mb.Entity<Endereco>(e =>
        {
            e.Property(p => p.EnderecoId)
                .HasDefaultValueSql("NEXT VALUE FOR seq_endereco");
        });

        mb.Entity<Contato>(ct =>
        {
            ct.Property(p => p.ContatoId)
                .HasDefaultValueSql("NEXT VALUE FOR seq_contato");
        });
    }
}
