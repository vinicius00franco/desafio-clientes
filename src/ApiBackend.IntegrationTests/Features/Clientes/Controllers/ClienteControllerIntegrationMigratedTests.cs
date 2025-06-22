using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Features.Clientes.Dtos.Contatos;
using ApiBackend.IntegrationTests.Infrastructure;

namespace ApiBackend.IntegrationTests.Features.Clientes.Controllers;

/// <summary>
/// Integration tests migrated from unit tests in ClienteControllerTests.cs
/// These tests execute against a real in-memory database instead of using mocks
/// </summary>
public class ClienteControllerIntegrationMigratedTests : IntegrationTestBase
{

    #region CriarCliente Tests

    [Fact]
    public async Task CriarCliente_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        var dto = CriarNovoClienteDtoValido();

        // Act
        var response = await Client.PostAsJsonAsync("/api/clientes", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        // Verificar estrutura da resposta JSON
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        responseObj.Should().ContainKey("id");
        
        var clienteId = JsonSerializer.Deserialize<int>(responseObj["id"].ToString()!);
        clienteId.Should().BePositive();
        
        // Verificar que o cliente foi realmente criado no banco
        var getResponse = await Client.GetAsync($"/api/clientes/{clienteId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CriarCliente_DeveRetornar400_QuandoCepInvalido()
    {
        // Arrange
        var dto = new
        {
            nome = "João Silva",
            cep = "00000-000", // CEP inválido que deve causar InvalidOperationException
            numero = "123",
            complemento = "Apt 45",
            contatos = new[]
            {
                new { tipo = "Email", valor = "joao.silva@email.com" },
                new { tipo = "Telefone", valor = "11999999999" }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/clientes", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        // Verificar estrutura da resposta JSON de erro
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        responseObj.Should().ContainKey("erro");
    }

    [Fact]
    public async Task CriarCliente_DeveRetornar400_QuandoDadosInvalidos()
    {
        // Arrange - DTO com dados inválidos (nome vazio)
        var dto = new
        {
            nome = "", // Nome vazio deve causar erro de validação
            cep = "01310-100",
            numero = "123",
            complemento = "Apt 45",
            contatos = new[]
            {
                new { tipo = "Email", valor = "joao.silva@email.com" }
            }
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/clientes", dto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region ObterPorId Tests

    [Fact]
    public async Task ObterPorId_DeveRetornar200_QuandoClienteExiste()
    {
        // Arrange - Primeiro criar um cliente com nome único para este teste
        var uniqueName = $"João Silva Test {Guid.NewGuid():N}";
        var dto = CriarNovoClienteDtoValido(uniqueName, $"test.{Guid.NewGuid():N}@email.com");
        var createResponse = await Client.PostAsJsonAsync("/api/clientes", dto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createObj = JsonSerializer.Deserialize<Dictionary<string, object>>(createContent);
        var clienteId = JsonSerializer.Deserialize<int>(createObj["id"].ToString()!);

        // Act
        var response = await Client.GetAsync($"/api/clientes/{clienteId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var cliente = JsonSerializer.Deserialize<Cliente>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Verificar estrutura do objeto Cliente retornado
        cliente.Should().NotBeNull();
        cliente!.ClienteId.Should().Be(clienteId);
        cliente.Nome.Should().Be(uniqueName); // Use unique name for this test
        cliente.DataCadastroUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
        cliente.Enderecos.Should().NotBeNull().And.HaveCount(1);
        cliente.Contatos.Should().NotBeNull().And.HaveCount(2);
        
        // Verificar estrutura dos endereços
        var endereco = cliente.Enderecos.First();
        endereco.EnderecoId.Should().BePositive();
        endereco.Logradouro.Should().NotBeNullOrEmpty();
        endereco.Numero.Should().Be("123");
        endereco.Bairro.Should().NotBeNullOrEmpty();
        endereco.Cidade.Should().NotBeNullOrEmpty();
        endereco.Estado.Should().NotBeNullOrEmpty();
        endereco.Cep.Should().Be("01310-100");
        endereco.ClienteId.Should().Be(clienteId);
        
        // Verificar estrutura dos contatos
        cliente.Contatos.Should().HaveCount(2);
        var emailContato = cliente.Contatos.FirstOrDefault(c => c.Tipo == "Email");
        var telefoneContato = cliente.Contatos.FirstOrDefault(c => c.Tipo == "Telefone");
        
        emailContato.Should().NotBeNull();
        emailContato!.Valor.Should().Be("joao.silva@email.com");
        emailContato.ClienteId.Should().Be(clienteId);
        
        telefoneContato.Should().NotBeNull();
        telefoneContato!.Valor.Should().Be("11999999999");
        telefoneContato.ClienteId.Should().Be(clienteId);
    }

    [Fact]
    public async Task ObterPorId_DeveRetornar404_QuandoClienteNaoExiste()
    {
        // Arrange
        var clienteId = 999;

        // Act
        var response = await Client.GetAsync($"/api/clientes/{clienteId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().NotBeNullOrEmpty();
        
        // Verificar estrutura da resposta JSON de erro
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
        responseObj.Should().ContainKey("erro");
        
        var erro = responseObj["erro"].ToString();
        erro.Should().Contain("Cliente não encontrado");
    }

    #endregion

    #region ListarTodos Tests

    [Fact]
    public async Task ListarTodos_DeveRetornar200_ComListaDeClientes()
    {
        // Arrange - Criar múltiplos clientes
        var clientes = new[]
        {
            CriarNovoClienteDtoValido("João Silva", "joao.silva@email.com"),
            CriarNovoClienteDtoValido("Maria Santos", "maria.santos@email.com"),
            CriarNovoClienteDtoValido("Pedro Oliveira", "pedro.oliveira@email.com")
        };

        var clienteIds = new List<int>();
        foreach (var dto in clientes)
        {
            var createResponse = await Client.PostAsJsonAsync("/api/clientes", dto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            var createContent = await createResponse.Content.ReadAsStringAsync();
            var createObj = JsonSerializer.Deserialize<Dictionary<string, object>>(createContent);
            var clienteId = JsonSerializer.Deserialize<int>(createObj["id"].ToString()!);
            clienteIds.Add(clienteId);
        }

        // Act
        var response = await Client.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var clientesRetornados = JsonSerializer.Deserialize<List<Cliente>>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Verificar que retorna lista de clientes
        clientesRetornados.Should().NotBeNull();
        clientesRetornados!.Should().HaveCountGreaterOrEqualTo(3);
        
        // Verificar que todos os clientes criados estão na lista
        foreach (var clienteId in clienteIds)
        {
            clientesRetornados.Should().Contain(c => c.ClienteId == clienteId);
        }
        
        // Verificar estrutura de cada cliente na lista
        foreach (var cliente in clientesRetornados.Where(c => clienteIds.Contains(c.ClienteId)))
        {
            cliente.ClienteId.Should().BePositive();
            cliente.Nome.Should().NotBeNullOrEmpty();
            cliente.DataCadastroUtc.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
            cliente.Enderecos.Should().NotBeNull();
            cliente.Contatos.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task ListarTodos_DeveRetornar200_ComListaVazia_QuandoNaoHaClientes()
    {
        // Arrange - Garantir que não há clientes (banco limpo)
        // O banco já é limpo entre testes pelo IntegrationTestBase

        // Act
        var response = await Client.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var clientesRetornados = JsonSerializer.Deserialize<List<Cliente>>(responseContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        
        // Verificar que retorna lista vazia mas válida
        clientesRetornados.Should().NotBeNull();
        clientesRetornados!.Should().BeEmpty();
    }

    #endregion

    #region Testes de Headers e Content-Type

    [Fact]
    public async Task TodosOsEndpoints_DeveRetornarJSON()
    {
        // Arrange
        var dto = CriarNovoClienteDtoValido();

        // Act & Assert para CriarCliente
        var responsePost = await Client.PostAsJsonAsync("/api/clientes", dto);
        responsePost.StatusCode.Should().Be(HttpStatusCode.Created);
        responsePost.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var createContent = await responsePost.Content.ReadAsStringAsync();
        var createObj = JsonSerializer.Deserialize<Dictionary<string, object>>(createContent);
        var clienteId = JsonSerializer.Deserialize<int>(createObj["id"].ToString()!);

        // Act & Assert para ObterPorId
        var responseGet = await Client.GetAsync($"/api/clientes/{clienteId}");
        responseGet.StatusCode.Should().Be(HttpStatusCode.OK);
        responseGet.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var getContent = await responseGet.Content.ReadAsStringAsync();
        var cliente = JsonSerializer.Deserialize<Cliente>(getContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        cliente.Should().NotBeNull();

        // Act & Assert para ListarTodos
        var responseGetAll = await Client.GetAsync("/api/clientes");
        responseGetAll.StatusCode.Should().Be(HttpStatusCode.OK);
        responseGetAll.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        
        var getAllContent = await responseGetAll.Content.ReadAsStringAsync();
        var clientes = JsonSerializer.Deserialize<List<Cliente>>(getAllContent, new JsonSerializerOptions 
        { 
            PropertyNameCaseInsensitive = true 
        });
        clientes.Should().NotBeNull();
    }

    #endregion

    #region Métodos Auxiliares

    private static object CriarNovoClienteDtoValido(string nome = "João Silva", string email = "joao.silva@email.com")
    {
        return new
        {
            nome = nome,
            cep = "01310-100",
            numero = "123",
            complemento = "Apt 45",
            contatos = new[]
            {
                new { tipo = "Email", valor = email },
                new { tipo = "Telefone", valor = "11999999999" }
            }
        };
    }

    #endregion
}
