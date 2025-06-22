using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace ApiBackend.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory customizada para testes de integração com LocalDB
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;

    public CustomWebApplicationFactory()
    {
        // Gerar nome único do banco para cada execução de teste
        _databaseName = $"TestDb_{Guid.NewGuid():N}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Configurar para ambiente de teste
        builder.UseEnvironment("Testing");

        // Configurar services específicos para teste
        builder.ConfigureServices(services =>
        {
            // Remover o DbContext original se existir
            var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ContextoApp>));
            if (dbContextDescriptor != null)
                services.Remove(dbContextDescriptor);

            var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ContextoApp));
            if (dbContextServiceDescriptor != null)
                services.Remove(dbContextServiceDescriptor);

            // Adicionar DbContext configurado para LocalDB com nome único
            services.AddDbContext<ContextoApp>(options =>
            {
                var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
                options.UseSqlServer(connectionString);
            });
        });

        // Configurar logging específico para testes
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            // Adicionar apenas console para debug se necessário
            // logging.AddConsole();
        });
    }

    /// <summary>
    /// Inicializa o banco de dados aplicando migrations
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextoApp>();
        
        // Garantir que o banco seja criado e as migrations aplicadas
        await context.Database.EnsureCreatedAsync();
        
        // Aplicar migrations se necessário
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync();
        }
    }

    /// <summary>
    /// Limpa o banco de dados ao final dos testes
    /// </summary>
    public async Task CleanupDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextoApp>();
        
        try
        {
            await context.Database.EnsureDeletedAsync();
        }
        catch (Exception ex)
        {
            // Log do erro mas não falhar o teste
            Console.WriteLine($"Erro ao limpar banco de dados {_databaseName}: {ex.Message}");
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cleanup assíncrono não pode ser chamado no Dispose
            // Será feito no IAsyncLifetime dos testes
        }
        base.Dispose(disposing);
    }
}
