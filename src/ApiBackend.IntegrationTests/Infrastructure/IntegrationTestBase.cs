namespace ApiBackend.IntegrationTests.Infrastructure;

/// <summary>
/// Classe base para testes de integração que fornece uma WebApplicationFactory configurada
/// </summary>
public class IntegrationTestBase : IAsyncLifetime
{
    protected CustomWebApplicationFactory Factory { get; private set; } = default!;
    protected HttpClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        Factory = new CustomWebApplicationFactory();
        Client = Factory.CreateClient();
        
        // Inicializar o banco de dados com schema
        await Factory.InitializeDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        // Limpar banco de dados
        if (Factory != null)
        {
            await Factory.CleanupDatabaseAsync();
            Factory.Dispose();
        }
        
        Client?.Dispose();
    }

    /// <summary>
    /// Obtém uma instância do DbContext para acesso direto ao banco em testes
    /// </summary>
    protected ContextoApp GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<ContextoApp>();
    }

    /// <summary>
    /// Helper para criar DTOs válidos para testes
    /// </summary>
    protected static NovoClienteDto CriarNovoClienteDtoValido(
        string nome = "João Silva",
        string cep = "01310-100",
        string numero = "123",
        string? complemento = "Apt 45")
    {
        return new NovoClienteDto(
            Nome: nome,
            Cep: cep,
            Numero: numero,
            Complemento: complemento,
            Contatos: new List<NovoContatoDto>
            {
                new EmailContatoDto("joao.silva@email.com"),
                new TelefoneContatoDto("11999999999")
            }
        );
    }
}
