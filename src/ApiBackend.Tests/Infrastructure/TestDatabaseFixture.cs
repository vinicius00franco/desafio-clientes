using ApiBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.MsSql;

namespace ApiBackend.Tests.Infrastructure;

/// <summary>
/// Fixture responsável por configurar o ambiente de teste com SQL Server usando Testcontainers.
/// Garante isolamento completo e usa o mesmo engine do ambiente de produção.
/// </summary>
public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _msSqlContainer;
    
    public string ConnectionString => _msSqlContainer.GetConnectionString();
    
    public TestDatabaseFixture()
    {
        // Configuração do container SQL Server identico ao usado em produção
        _msSqlContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest") // Mesma imagem do docker-compose.yml
            .WithPassword("TestPass#Seguro123") // Senha complexa para os testes
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithAutoRemove(true) // Remove o container automaticamente após os testes
            .WithCleanUp(true) // Limpa recursos automaticamente
            .Build();
    }

    /// <summary>
    /// Inicializa o container e aplica as migrations do Entity Framework.
    /// </summary>
    public async Task InitializeAsync()
    {
        // Inicia o container SQL Server
        await _msSqlContainer.StartAsync();

        // Aguarda o SQL Server estar completamente pronto
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Aplica todas as migrations para garantir que o schema está atualizado
        await ApplyMigrationsAsync();
    }

    /// <summary>
    /// Para e remove o container após os testes.
    /// </summary>
    public async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Cria um novo contexto de banco configurado para o container de teste.
    /// </summary>
    public ContextoApp CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ContextoApp>()
            .UseSqlServer(ConnectionString)
            .EnableServiceProviderCaching(false) // Desabilita cache para testes isolados
            .EnableSensitiveDataLogging() // Habilita logs detalhados para debug
            .LogTo(Console.WriteLine, LogLevel.Information) // Log para console durante testes
            .Options;

        return new ContextoApp(options);
    }

    /// <summary>
    /// Aplica todas as migrations do Entity Framework ao banco de teste.
    /// </summary>
    private async Task ApplyMigrationsAsync()
    {
        using var context = CreateDbContext();
        
        // Garante que o banco de dados existe
        await context.Database.EnsureCreatedAsync();
        
        // Aplica todas as migrations pendentes
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync();
        }
        
        // Verifica se as migrations foram aplicadas corretamente
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Console.WriteLine($"Migrations aplicadas: {string.Join(", ", appliedMigrations)}");
    }

    /// <summary>
    /// Limpa todos os dados das tabelas, mantendo a estrutura.
    /// Útil para isolar testes sem recriar o banco.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        using var context = CreateDbContext();
        
        // Remove dados das tabelas filhas primeiro (FK constraints)
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Contatos");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Enderecos");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM HistoricoClientes");
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Clientes");
        
        // Reset das sequences para começar do ID 1 novamente
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Clientes', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Enderecos', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Contatos', RESEED, 0)");
        await context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('HistoricoClientes', RESEED, 0)");
    }

    /// <summary>
    /// Verifica se o container está rodando e acessível.
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var context = CreateDbContext();
            return await context.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
}
