using ApiBackend.Data;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace ApiBackend.IntegrationTests.Features.Clientes.Controllers;

public class ClienteControllerIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task POST_Clientes_DeveRetornar201_QuandoClienteValido()
    {
        // Arrange
        var novoClienteDto = new
        {
            nome = "João Silva",
            cep = "01310-100",
            numero = "123",
            complemento = "Apt 45",
            contatos = new[]
            {
                new { tipo = "Email", valor = "joao.silva@teste.com" },
                new { tipo = "Telefone", valor = "11999999999" }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/clientes", novoClienteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var location = response.Headers.Location;
        location.Should().NotBeNull();
        location!.PathAndQuery.Should().StartWith("/api/clientes/");
    }

    [Fact]
    public async Task GET_Clientes_DeveRetornar200_ComListaVazia_QuandoNaoHaClientes()
    {
        // Act
        var response = await Client.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }

    [Fact]
    public async Task GET_Clientes_DeveRetornar200_ComListaClientes_QuandoHaClientes()
    {
        // Arrange - Criar cliente diretamente no banco para teste
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextoApp>();
        
        var cliente = new Cliente
        {
            Nome = "Maria Santos",
            DataCadastroUtc = DateTime.UtcNow,
            Enderecos = new List<Endereco>
            {
                new() 
                {
                    Logradouro = "Rua das Flores",
                    Numero = "123",
                    Bairro = "Centro",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    Cep = "01000-000"
                }
            },
            Contatos = new List<Contato>
            {
                new() { Tipo = "Email", Valor = "maria.santos@teste.com" },
                new() { Tipo = "Telefone", Valor = "11988888888" }
            }
        };
        
        context.Clientes.Add(cliente);
        await context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Maria Santos");
        content.Should().Contain("maria.santos@teste.com");
    }

    [Fact]
    public async Task POST_Clientes_DeveRetornar400_QuandoClienteInvalido()
    {
        // Arrange
        var clienteInvalido = new
        {
            nome = "", // Nome vazio
            cep = "00000-000", // CEP inválido
            numero = "123",
            contatos = new[]
            {
                new { tipo = "Email", valor = "email-invalido" }, // Email inválido
                new { tipo = "Telefone", valor = "123" } // Telefone muito curto
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/clientes", clienteInvalido);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Database_DeveEstarIsolado_EntreTestesDiferentes()
    {
        // Arrange & Act
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextoApp>();
        
        var clientesCount = await context.Clientes.CountAsync();

        // Assert
        // O banco deve estar vazio em cada teste (exceto nos testes onde criamos dados especificamente)
        clientesCount.Should().Be(0);
    }
}
