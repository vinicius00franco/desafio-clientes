using System.Net;

namespace ApiBackend.IntegrationTests.Features.Clientes.Controllers;

/// <summary>
/// Testes de integração básicos para verificar se a infraestrutura está funcionando
/// </summary>
public class ClienteControllerIntegrationBasicTests : IntegrationTestBase
{
    [Fact]
    public async Task Setup_DeveInicializarCorretamente()
    {
        // Arrange & Act
        var response = await Client.GetAsync("/api/clientes");

        // Assert
        response.Should().NotBeNull();
        // O endpoint pode retornar 200 com lista vazia ou outro status, 
        // o importante é que não dê erro de infraestrutura
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Database_DeveEstarConfiguradoCorretamente()
    {
        // Arrange & Act
        using var context = GetDbContext();
        
        // Assert
        context.Should().NotBeNull();
        context.Database.Should().NotBeNull();
        
        // Verificar se consegue acessar as tabelas (vai dar erro se schema não existir)
        var clientesCount = await context.Clientes.CountAsync();
        clientesCount.Should().BeGreaterThanOrEqualTo(0);
    }
}
