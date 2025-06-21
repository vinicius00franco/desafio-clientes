using ApiBackend.Data;
using ApiBackend.Features.Clientes.Repositories;
using ApiBackend.Features.Clientes.Services;
using ApiBackend.Services.External;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ApiBackend.Tests.Infrastructure;

namespace ApiBackend.Tests.Infrastructure;

/// <summary>
/// Factory para criar serviços configurados para testes de integração.
/// Centraliza a configuração de dependências e mocks necessários.
/// </summary>
public class TestServiceFactory : IDisposable
{
    private readonly TestDatabaseFixture _databaseFixture;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<IViaCepService> _viaCepServiceMock;

    public TestServiceFactory(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        _viaCepServiceMock = new Mock<IViaCepService>();
        
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Configura todos os serviços necessários para os testes, incluindo mocks.
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        // Configuração de logging para testes
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Contexto do banco de dados usando o Testcontainer
        services.AddScoped<ContextoApp>(_ => _databaseFixture.CreateDbContext());

        // AutoMapper com os perfis da aplicação
        services.AddAutoMapper(typeof(ApiBackend.Features.Clientes.ClienteMapper));

        // Repositories
        services.AddScoped<ClienteRepository>();

        // Mock do ViaCepService para controlar retornos nos testes
        services.AddScoped<IViaCepService>(_ => _viaCepServiceMock.Object);

        // Services da aplicação
        services.AddScoped<ClienteService>();
    }

    /// <summary>
    /// Obtém uma instância do serviço solicitado.
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Obtém o mock do ViaCepService para configurar comportamentos específicos nos testes.
    /// </summary>
    public Mock<IViaCepService> GetViaCepServiceMock() => _viaCepServiceMock;

    /// <summary>
    /// Cria um novo escopo de serviços isolado.
    /// Útil para testes que precisam de instâncias independentes.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return _serviceProvider.CreateScope();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
