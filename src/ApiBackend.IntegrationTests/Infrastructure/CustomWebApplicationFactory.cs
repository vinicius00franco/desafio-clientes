using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace ApiBackend.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory customizada para testes de integração com banco em memória
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

            // Remover todos os serviços do Entity Framework SqlServer existentes
            var descriptorsToRemove = services.Where(d => 
                d.ServiceType.FullName?.Contains("Microsoft.EntityFrameworkCore") == true ||
                d.ImplementationType?.FullName?.Contains("Microsoft.EntityFrameworkCore.SqlServer") == true)
                .ToList();
            
            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Remover o serviço de inicialização do banco para evitar execução de migrations em testes
            var dbInitializerDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApiBackend.Data.Services.IDatabaseInitializationService));
            if (dbInitializerDescriptor != null)
                services.Remove(dbInitializerDescriptor);

            // Adicionar DbContext configurado para InMemory com nome único
            services.AddDbContext<ContextoApp>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            // Adicionar um mock do DatabaseInitializationService que não faz nada
            services.AddScoped<ApiBackend.Data.Services.IDatabaseInitializationService, MockDatabaseInitializationService>();
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
    /// Inicializa o banco de dados criando as entidades necessárias
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextoApp>();
        
        // Para InMemory, garantir que o banco está criado
        await context.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Limpa o banco de dados ao final dos testes (InMemory é automaticamente limpo)
    /// </summary>
    public async Task CleanupDatabaseAsync()
    {
        // Para InMemory Database, não é necessário cleanup manual
        // O banco é automaticamente removido quando o contexto é disposto
        await Task.CompletedTask;
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

/// <summary>
/// Mock do DatabaseInitializationService que não executa migrations para testes
/// </summary>
public class MockDatabaseInitializationService : ApiBackend.Data.Services.IDatabaseInitializationService
{
    public async Task Initialize()
    {
        // Para testes de integração, não executar migrations nem scripts
        // O banco InMemory já é criado automaticamente com EnsureCreated
        await Task.CompletedTask;
    }
}
