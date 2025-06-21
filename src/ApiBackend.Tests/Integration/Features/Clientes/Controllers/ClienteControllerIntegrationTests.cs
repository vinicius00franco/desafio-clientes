using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiBackend.Data;
using ApiBackend.Services.External;

namespace ApiBackend.Tests.Integration.Features.Clientes.Controllers;

/// <summary>
/// Testes de integração end-to-end para ClienteController usando TestContainer e WebApplicationFactory.
/// Testa o pipeline completo: HTTP → Controller → Service → Repository → Database.
/// Inclui testes de:
/// - HTTP endpoints com diferentes cenários
/// - Serialização/deserialização JSON
/// - Status codes e headers HTTP
/// - Integração completa com banco real
/// </summary>
[Collection("Database")]
public class ClienteControllerIntegrationTests : IClassFixture<ClienteApiWebApplicationFactory>, IAsyncLifetime
{
    private readonly ClienteApiWebApplicationFactory _factory;
    private readonly HttpClient _httpClient;
    private readonly TestDatabaseFixture _databaseFixture;

    public ClienteControllerIntegrationTests(
        ClienteApiWebApplicationFactory factory, 
        TestDatabaseFixture databaseFixture)
    {
        _factory = factory;
        _databaseFixture = databaseFixture;
        _httpClient = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Configura o factory para usar o banco de teste
        _factory.ConfigureDatabase(_databaseFixture.ConnectionString);
        
        var isHealthy = await _databaseFixture.IsHealthyAsync();
        isHealthy.Should().BeTrue("O banco de dados deve estar acessível");
        
        await _databaseFixture.CleanDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task POST_Clientes_ComDadosValidos_DeveRetornar201ELinkParaRecurso()
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .ComNome("João Silva API")
            .ComCep("01310-100")
            .ComNumero("1000")
            .ComComplemento("Sala 101")
            .ComContatos(
                new NovoContatoDto("Email", "joao.api@teste.com"),
                new NovoContatoDto("Telefone", "(11) 99999-8888")
            )
            .Build();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/clientes", novoClienteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, "Deve retornar 201 Created");
        
        // Verificar Location header
        response.Headers.Location.Should().NotBeNull("Deve ter Location header");
        response.Headers.Location!.ToString().Should().Match("/api/clientes/*", "Location deve apontar para o novo recurso");

        // Verificar corpo da resposta
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        responseObject.GetProperty("id").GetInt32().Should().BeGreaterThan(0, "Deve retornar o ID do cliente criado");

        // Verificar se foi realmente persistido no banco
        var clienteId = responseObject.GetProperty("id").GetInt32();
        using var context = _databaseFixture.CreateDbContext();
        var clienteSalvo = await context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

        clienteSalvo.Should().NotBeNull("Cliente deve estar salvo no banco");
        clienteSalvo!.Nome.Should().Be("João Silva API");
        clienteSalvo.Enderecos.Should().HaveCount(1);
        clienteSalvo.Contatos.Should().HaveCount(2);
    }

    [Fact]
    public async Task POST_Clientes_ComCepInvalido_DeveRetornar400ComMensagemErro()
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .ComCep("99999-999") // CEP inválido
            .AdicionarContato("Email", "teste@email.com")
            .Build();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/clientes", novoClienteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "Deve retornar 400 Bad Request");

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        responseObject.TryGetProperty("erro", out var erroProperty).Should().BeTrue("Deve ter propriedade 'erro'");
        erroProperty.GetString().Should().Contain("99999-999", "Mensagem deve mencionar o CEP inválido");
        erroProperty.GetString().Should().Contain("não encontrado", "Mensagem deve explicar o problema");
    }

    [Fact]
    public async Task POST_Clientes_SemContatos_DeveRetornar400()
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .SemContatos()
            .Build();

        // Act
        var response = await _httpClient.PostAsJsonAsync("/api/clientes", novoClienteDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        responseObject.GetProperty("erro").GetString()
            .Should().Contain("contato é obrigatório");
    }

    [Fact]
    public async Task POST_Clientes_ComJsonInvalido_DeveRetornar400()
    {
        // Arrange - JSON malformado
        var jsonInvalido = "{ \"nome\": \"Teste\", \"cep\": }"; // JSON inválido

        // Act
        var response = await _httpClient.PostAsync("/api/clientes", 
            new StringContent(jsonInvalido, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Clientes_Id_ComIdExistente_DeveRetornar200ComClienteCompleto()
    {
        // Arrange - Criar cliente no banco
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Maria API Test")
            .ComEndereco("12345-678", "Avenida Teste", "Bairro Teste", "São Paulo", "SP", "456")
            .ComContato("Email", "maria.api@teste.com")
            .ComContato("Telefone", "(11) 88888-7777")
            .ComContato("WhatsApp", "(11) 88888-7777")
            .Build();

        int clienteId;
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        // Act
        var response = await _httpClient.GetAsync($"/api/clientes/{clienteId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var clienteRetornado = JsonSerializer.Deserialize<JsonElement>(responseContent);

        // Verificar propriedades do cliente
        clienteRetornado.GetProperty("clienteId").GetInt32().Should().Be(clienteId);
        clienteRetornado.GetProperty("nome").GetString().Should().Be("Maria API Test");
        
        // Verificar endereços
        var enderecos = clienteRetornado.GetProperty("enderecos").EnumerateArray().ToList();
        enderecos.Should().HaveCount(1);
        var endereco = enderecos.First();
        endereco.GetProperty("cep").GetString().Should().Be("12345-678");
        endereco.GetProperty("logradouro").GetString().Should().Be("Avenida Teste");
        endereco.GetProperty("cidade").GetString().Should().Be("São Paulo");
        endereco.GetProperty("numero").GetString().Should().Be("456");

        // Verificar contatos
        var contatos = clienteRetornado.GetProperty("contatos").EnumerateArray().ToList();
        contatos.Should().HaveCount(3);
        
        var contatosValores = contatos.Select(c => new 
        {
            Tipo = c.GetProperty("tipo").GetString(),
            Valor = c.GetProperty("valor").GetString()
        }).ToList();

        contatosValores.Should().Contain(c => c.Tipo == "Email" && c.Valor == "maria.api@teste.com");
        contatosValores.Should().Contain(c => c.Tipo == "Telefone" && c.Valor == "(11) 88888-7777");
        contatosValores.Should().Contain(c => c.Tipo == "WhatsApp" && c.Valor == "(11) 88888-7777");
    }

    [Fact]
    public async Task GET_Clientes_Id_ComIdInexistente_DeveRetornar404()
    {
        // Arrange
        const int idInexistente = 999999;

        // Act
        var response = await _httpClient.GetAsync($"/api/clientes/{idInexistente}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        responseObject.GetProperty("erro").GetString()
            .Should().Contain("Cliente não encontrado");
    }

    [Theory]
    [InlineData("/api/clientes/0")]
    [InlineData("/api/clientes/-1")]
    [InlineData("/api/clientes/abc")]
    public async Task GET_Clientes_Id_ComIdInvalido_DeveRetornar400Ou404(string url)
    {
        // Act
        var response = await _httpClient.GetAsync(url);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Clientes_ComClientesNoBanco_DeveRetornar200ComLista()
    {
        // Arrange - Criar múltiplos clientes
        var clientes = TestDataBuilder.CriarListaClientes(3);
        
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.AddRange(clientes);
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _httpClient.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var responseContent = await response.Content.ReadAsStringAsync();
        var clientesArray = JsonSerializer.Deserialize<JsonElement>(responseContent);

        clientesArray.ValueKind.Should().Be(JsonValueKind.Array, "Resposta deve ser um array");
        
        var clientesList = clientesArray.EnumerateArray().ToList();
        clientesList.Should().HaveCount(3, "Deve retornar todos os clientes");

        // Verificar se todos têm relacionamentos
        foreach (var cliente in clientesList)
        {
            cliente.GetProperty("clienteId").GetInt32().Should().BeGreaterThan(0);
            cliente.GetProperty("nome").GetString().Should().NotBeNullOrEmpty();
            
            var enderecos = cliente.GetProperty("enderecos").EnumerateArray();
            enderecos.Should().NotBeEmpty("Cada cliente deve ter endereços");
            
            var contatos = cliente.GetProperty("contatos").EnumerateArray();
            contatos.Should().NotBeEmpty("Cada cliente deve ter contatos");
        }

        // Verificar nomes específicos
        var nomes = clientesList.Select(c => c.GetProperty("nome").GetString()).ToList();
        nomes.Should().Contain("Cliente 1");
        nomes.Should().Contain("Cliente 2");
        nomes.Should().Contain("Cliente 3");
    }

    [Fact]
    public async Task GET_Clientes_SemClientesNoBanco_DeveRetornar200ComArrayVazio()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/clientes");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var clientesArray = JsonSerializer.Deserialize<JsonElement>(responseContent);

        clientesArray.ValueKind.Should().Be(JsonValueKind.Array);
        clientesArray.EnumerateArray().Should().BeEmpty("Array deve estar vazio");
    }

    [Fact]
    public async Task FluxoCompleto_CriarObterListar_DeveManterConsistencia()
    {
        // Arrange
        var novoCliente1 = TestDataBuilder.NovoCliente()
            .ComNome("Cliente Fluxo 1")
            .AdicionarContato("Email", "fluxo1@teste.com")
            .Build();

        var novoCliente2 = TestDataBuilder.NovoCliente()
            .ComNome("Cliente Fluxo 2")
            .AdicionarContato("Email", "fluxo2@teste.com")
            .Build();

        // Act 1 - Criar primeiro cliente
        var response1 = await _httpClient.PostAsJsonAsync("/api/clientes", novoCliente1);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var cliente1Id = JsonSerializer.Deserialize<JsonElement>(content1).GetProperty("id").GetInt32();

        // Act 2 - Criar segundo cliente
        var response2 = await _httpClient.PostAsJsonAsync("/api/clientes", novoCliente2);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content2 = await response2.Content.ReadAsStringAsync();
        var cliente2Id = JsonSerializer.Deserialize<JsonElement>(content2).GetProperty("id").GetInt32();

        // Act 3 - Obter primeiro cliente por ID
        var responseGet = await _httpClient.GetAsync($"/api/clientes/{cliente1Id}");
        responseGet.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var clienteObtido = JsonSerializer.Deserialize<JsonElement>(await responseGet.Content.ReadAsStringAsync());
        clienteObtido.GetProperty("nome").GetString().Should().Be("Cliente Fluxo 1");

        // Act 4 - Listar todos
        var responseList = await _httpClient.GetAsync("/api/clientes");
        responseList.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var todosClientes = JsonSerializer.Deserialize<JsonElement>(await responseList.Content.ReadAsStringAsync());
        var clientesList = todosClientes.EnumerateArray().ToList();

        // Assert final
        clientesList.Should().HaveCount(2, "Deve ter ambos os clientes criados");
        
        var nomes = clientesList.Select(c => c.GetProperty("nome").GetString()).ToList();
        nomes.Should().Contain("Cliente Fluxo 1");
        nomes.Should().Contain("Cliente Fluxo 2");

        var ids = clientesList.Select(c => c.GetProperty("clienteId").GetInt32()).ToList();
        ids.Should().Contain(cliente1Id);
        ids.Should().Contain(cliente2Id);
    }
}

/// <summary>
/// WebApplicationFactory customizada para testes de integração da API.
/// Configura o ambiente de teste com banco SQL Server real via Testcontainers.
/// </summary>
public class ClienteApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private string? _connectionString;

    public void ConfigureDatabase(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            if (_connectionString != null)
            {
                // Remove o DbContext original
                services.RemoveAll(typeof(DbContextOptions<ContextoApp>));
                services.RemoveAll(typeof(ContextoApp));

                // Adiciona DbContext com connection string do teste
                services.AddDbContext<ContextoApp>(options =>
                    options.UseSqlServer(_connectionString));

                // Mock do ViaCepService para testes controlados
                var viaCepMock = new Mock<IViaCepService>();
                viaCepMock.Setup(x => x.ObterPorCep(It.IsAny<string>()))
                    .ReturnsAsync(TestDataBuilder.CriarEnderecoCepDto());

                services.RemoveAll<IViaCepService>();
                services.AddSingleton(viaCepMock.Object);
            }
        });

        builder.UseEnvironment("Testing");
    }
}
