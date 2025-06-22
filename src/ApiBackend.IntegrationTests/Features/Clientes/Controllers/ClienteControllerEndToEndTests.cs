using ApiBackend.Data;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace ApiBackend.IntegrationTests.Features.Clientes.Controllers;

/// <summary>
/// Testes de integração do ClienteController:
/// - Criação de cliente válido (201 Created e header Location)
/// - Listagem de clientes (lista vazia e com itens)
/// - Validações de entrada inválida (400 BadRequest)
/// - Isolamento do banco entre testes
/// </summary>
public class ClienteControllerIntregracaoTests : IntegrationTestBase
{
    [Fact]
    // Testa criação de um cliente válido e verifica se retorna 201 Created com Location
    public async Task POST_Clientes_DeveRetornar201_QuandoClienteValido()
    {
        // Arrange: DTO com todos os campos obrigatórios e contatos válidos
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

        // Act: envia requisição para o endpoint de clientes
        var response = await Client.PostAsJsonAsync("/api/clientes", novoClienteDto);

        // Assert: confere status e header Location apontando para o recurso criado
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var location = response.Headers.Location;
        location.Should().NotBeNull();
        location!.PathAndQuery.Should().StartWith("/api/clientes/");
    }

    [Fact]
    // Verifica GET retorna 200 e lista vazia quando não há clientes
    public async Task GET_Clientes_DeveRetornar200_ComListaVazia_QuandoNaoHaClientes()
    {
        // Act: consulta todos os clientes
        var response = await Client.GetAsync("/api/clientes");

        // Assert: verifica status OK e body igual a "[]"
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
    }

    [Fact]
    // Insere um cliente diretamente no banco e valida que o GET retorna seus dados
    public async Task GET_Clientes_DeveRetornar200_ComListaClientes_QuandoHaClientes()
    {
        // Arrange: seed de dados no banco para simular cliente existente
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

        // Act: busca lista de clientes via API
        var response = await Client.GetAsync("/api/clientes");

        // Assert: confirma status OK e presença do nome e email cadastrados
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Maria Santos");
        content.Should().Contain("maria.santos@teste.com");
    }

    [Fact]
    // Testa validação de entrada inválida e espera BadRequest
    public async Task POST_Clientes_DeveRetornar400_QuandoClienteInvalido()
    {
        // Arrange: DTO com nome vazio, CEP inválido e contatos mal formados
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

        // Act: envia dados inválidos e aguarda resposta
        var response = await Client.PostAsJsonAsync("/api/clientes", clienteInvalido);

        // Assert: confirma retorno de BadRequest
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    // Garante que cada teste execute em banco isolado, sem dados remanescentes
    public async Task Database_DeveEstarIsolado_EntreTestesDiferentes()
    {
        // Arrange & Act: obtém contagem de clientes no contexto atual
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ContextoApp>();
        
        var clientesCount = await context.Clientes.CountAsync();

        // Assert: espera zero clientes, evidenciando isolamento entre testes
        clientesCount.Should().Be(0);
    }
}
