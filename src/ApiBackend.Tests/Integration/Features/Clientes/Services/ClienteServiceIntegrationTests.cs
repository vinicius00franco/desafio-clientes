using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Services;
using ApiBackend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace ApiBackend.Tests.Integration.Features.Clientes.Services;

/// <summary>
/// Testes de integração para ClienteService usando Testcontainers com SQL Server real.
/// Testa o comportamento completo do serviço com banco de dados, incluindo:
/// - Persistência de dados
/// - Relacionamentos entre entidades
/// - Integração com ViaCEP (mockado)
/// - Validações de negócio
/// - Mapeamentos AutoMapper
/// </summary>
[Collection("Database")]
public class ClienteServiceIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _databaseFixture;
    private readonly TestServiceFactory _serviceFactory;
    private readonly ClienteService _clienteService;

    public ClienteServiceIntegrationTests(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        _serviceFactory = new TestServiceFactory(_databaseFixture);
        _clienteService = _serviceFactory.GetService<ClienteService>();
    }

    public async Task InitializeAsync()
    {
        // Verifica se o banco está saudável antes de cada teste
        var isHealthy = await _databaseFixture.IsHealthyAsync();
        isHealthy.Should().BeTrue("O banco de dados deve estar acessível");

        // Limpa dados de testes anteriores
        await _databaseFixture.CleanDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        _serviceFactory?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CriarCliente_ComDadosValidos_DevePersistirClienteCompletoNoBanco()
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .ComNome("João Silva")
            .ComCep("01310-100")
            .ComNumero("1000")
            .ComComplemento("Andar 10")
            .ComContatos(
                new NovoContatoDto("Email", "joao.silva@teste.com"),
                new NovoContatoDto("Telefone", "(11) 98765-4321"),
                new NovoContatoDto("WhatsApp", "(11) 98765-4321")
            )
            .Build();

        var enderecoCepDto = TestDataBuilder.CriarEnderecoCepDto(
            cep: "01310-100",
            logradouro: "Avenida Paulista",
            bairro: "Bela Vista",
            localidade: "São Paulo",
            uf: "SP"
        );

        // Mock do ViaCEP
        _serviceFactory.GetViaCepServiceMock()
            .Setup(x => x.ObterPorCep("01310-100"))
            .ReturnsAsync(enderecoCepDto);

        // Act
        var clienteId = await _clienteService.CriarCliente(novoClienteDto);

        // Assert
        clienteId.Should().BeGreaterThan(0, "O ID do cliente deve ser gerado automaticamente");

        // Verificar dados persistidos no banco
        using var context = _databaseFixture.CreateDbContext();
        
        var clienteSalvo = await context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

        // Assertions detalhadas do cliente
        clienteSalvo.Should().NotBeNull("O cliente deve estar salvo no banco");
        clienteSalvo!.Nome.Should().Be("João Silva");
        clienteSalvo.DataCadastroUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Assertions do endereço
        clienteSalvo.Enderecos.Should().HaveCount(1, "Deve ter exatamente um endereço");
        var endereco = clienteSalvo.Enderecos.First();
        endereco.Cep.Should().Be("01310-100");
        endereco.Logradouro.Should().Be("Avenida Paulista");
        endereco.Bairro.Should().Be("Bela Vista");
        endereco.Cidade.Should().Be("São Paulo");
        endereco.Estado.Should().Be("SP");
        endereco.Numero.Should().Be("1000");
        endereco.ClienteId.Should().Be(clienteId, "FK deve estar correta");

        // Assertions dos contatos
        clienteSalvo.Contatos.Should().HaveCount(3, "Deve ter todos os contatos informados");
        
        var contatoEmail = clienteSalvo.Contatos.FirstOrDefault(c => c.Tipo == "Email");
        contatoEmail.Should().NotBeNull("Deve ter contato de email");
        contatoEmail!.Valor.Should().Be("joao.silva@teste.com");
        contatoEmail.ClienteId.Should().Be(clienteId, "FK deve estar correta");

        var contatoTelefone = clienteSalvo.Contatos.FirstOrDefault(c => c.Tipo == "Telefone");
        contatoTelefone.Should().NotBeNull("Deve ter contato de telefone");
        contatoTelefone!.Valor.Should().Be("(11) 98765-4321");

        var contatoWhatsApp = clienteSalvo.Contatos.FirstOrDefault(c => c.Tipo == "WhatsApp");
        contatoWhatsApp.Should().NotBeNull("Deve ter contato de WhatsApp");
        contatoWhatsApp!.Valor.Should().Be("(11) 98765-4321");

        // Verificar que o ViaCEP foi chamado corretamente
        _serviceFactory.GetViaCepServiceMock()
            .Verify(x => x.ObterPorCep("01310-100"), Times.Once);
    }

    [Fact]
    public async Task CriarCliente_ComCepInvalido_DeveLancarExcecao()
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .ComCep("99999-999")
            .AdicionarContato("Email", "teste@email.com")
            .Build();

        // Mock retorna null para CEP inválido
        _serviceFactory.GetViaCepServiceMock()
            .Setup(x => x.ObterPorCep("99999-999"))
            .ReturnsAsync((EnderecoCepDto?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _clienteService.CriarCliente(novoClienteDto));

        exception.Message.Should().Contain("99999-999");
        exception.Message.Should().Contain("não encontrado");

        // Verificar que nada foi salvo no banco
        using var context = _databaseFixture.CreateDbContext();
        var clientesCount = await context.Clientes.CountAsync();
        clientesCount.Should().Be(0, "Nenhum cliente deve ser salvo quando o CEP é inválido");
    }

    [Fact]
    public async Task CriarCliente_SemContatos_DeveLancarExcecao()
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .SemContatos()
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _clienteService.CriarCliente(novoClienteDto));

        exception.Message.Should().Contain("contato é obrigatório");

        // Verificar que nada foi salvo no banco
        using var context = _databaseFixture.CreateDbContext();
        var clientesCount = await context.Clientes.CountAsync();
        clientesCount.Should().Be(0, "Nenhum cliente deve ser salvo quando não há contatos");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CriarCliente_ComNomeInvalido_DeveLancarExcecao(string nomeInvalido)
    {
        // Arrange
        var novoClienteDto = TestDataBuilder.NovoCliente()
            .ComNome(nomeInvalido)
            .AdicionarContato("Email", "teste@email.com")
            .Build();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _clienteService.CriarCliente(novoClienteDto));

        exception.Message.Should().Contain("Nome é obrigatório");

        // Verificar que nada foi salvo no banco
        using var context = _databaseFixture.CreateDbContext();
        var clientesCount = await context.Clientes.CountAsync();
        clientesCount.Should().Be(0, "Nenhum cliente deve ser salvo quando o nome é inválido");
    }

    [Fact]
    public async Task ObterPorId_ComIdExistente_DeveRetornarClienteCompleto()
    {
        // Arrange - Criar cliente diretamente no banco
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Maria Santos")
            .ComEndereco("12345-678", "Rua das Flores", "Centro", "Rio de Janeiro", "RJ", "456")
            .ComContato("Email", "maria@teste.com")
            .ComContato("Telefone", "(21) 91234-5678")
            .Build();

        int clienteId;
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        // Act
        var clienteEncontrado = await _clienteService.ObterPorId(clienteId);

        // Assert
        clienteEncontrado.Should().NotBeNull("Cliente deve ser encontrado");
        clienteEncontrado!.ClienteId.Should().Be(clienteId);
        clienteEncontrado.Nome.Should().Be("Maria Santos");

        // Verificar relacionamentos carregados
        clienteEncontrado.Enderecos.Should().HaveCount(1);
        var endereco = clienteEncontrado.Enderecos.First();
        endereco.Cep.Should().Be("12345-678");
        endereco.Logradouro.Should().Be("Rua das Flores");
        endereco.Cidade.Should().Be("Rio de Janeiro");
        endereco.Estado.Should().Be("RJ");

        clienteEncontrado.Contatos.Should().HaveCount(2);
        clienteEncontrado.Contatos.Should().Contain(c => c.Tipo == "Email" && c.Valor == "maria@teste.com");
        clienteEncontrado.Contatos.Should().Contain(c => c.Tipo == "Telefone" && c.Valor == "(21) 91234-5678");
    }

    [Fact]
    public async Task ObterPorId_ComIdInexistente_DeveRetornarNull()
    {
        // Arrange
        const int idInexistente = 99999;

        // Act
        var cliente = await _clienteService.ObterPorId(idInexistente);

        // Assert
        cliente.Should().BeNull("Cliente inexistente deve retornar null");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task ObterPorId_ComIdInvalido_DeveLancarExcecao(int idInvalido)
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _clienteService.ObterPorId(idInvalido));

        exception.Message.Should().Contain("ID deve ser maior que zero");
    }

    [Fact]
    public async Task ListarTodos_ComClientesNoBanco_DeveRetornarTodosComRelacionamentos()
    {
        // Arrange - Criar múltiplos clientes no banco
        var clientes = TestDataBuilder.CriarListaClientes(3);

        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.AddRange(clientes);
            await context.SaveChangesAsync();
        }

        // Act
        var clientesRetornados = await _clienteService.ListarTodos();

        // Assert
        var listaClientes = clientesRetornados.ToList();
        listaClientes.Should().HaveCount(3, "Deve retornar todos os clientes cadastrados");

        // Verificar se todos têm relacionamentos carregados
        foreach (var cliente in listaClientes)
        {
            cliente.Enderecos.Should().NotBeEmpty("Cada cliente deve ter endereços carregados");
            cliente.Contatos.Should().NotBeEmpty("Cada cliente deve ter contatos carregados");
        }

        // Verificar ordenação (implícita por ID)
        listaClientes.Should().BeInAscendingOrder(c => c.ClienteId, "Clientes devem estar ordenados por ID");

        // Verificar nomes específicos
        listaClientes.Should().Contain(c => c.Nome == "Cliente 1");
        listaClientes.Should().Contain(c => c.Nome == "Cliente 2");
        listaClientes.Should().Contain(c => c.Nome == "Cliente 3");
    }

    [Fact]
    public async Task ListarTodos_SemClientesNoBanco_DeveRetornarListaVazia()
    {
        // Arrange - Banco já está limpo pelo InitializeAsync

        // Act
        var clientes = await _clienteService.ListarTodos();

        // Assert
        clientes.Should().BeEmpty("Deve retornar lista vazia quando não há clientes");
    }

    [Fact]
    public async Task CriarCliente_ComMultiplosClientes_DevePersistirTodosComIdsSequenciais()
    {
        // Arrange
        var enderecoCepDto = TestDataBuilder.CriarEnderecoCepDto();
        _serviceFactory.GetViaCepServiceMock()
            .Setup(x => x.ObterPorCep(It.IsAny<string>()))
            .ReturnsAsync(enderecoCepDto);

        var cliente1Dto = TestDataBuilder.NovoCliente()
            .ComNome("Cliente 1")
            .AdicionarContato("Email", "cliente1@teste.com")
            .Build();

        var cliente2Dto = TestDataBuilder.NovoCliente()
            .ComNome("Cliente 2")
            .AdicionarContato("Email", "cliente2@teste.com")
            .Build();

        var cliente3Dto = TestDataBuilder.NovoCliente()
            .ComNome("Cliente 3")
            .AdicionarContato("Email", "cliente3@teste.com")
            .Build();

        // Act
        var id1 = await _clienteService.CriarCliente(cliente1Dto);
        var id2 = await _clienteService.CriarCliente(cliente2Dto);
        var id3 = await _clienteService.CriarCliente(cliente3Dto);

        // Assert
        id1.Should().Be(1, "Primeiro cliente deve ter ID 1");
        id2.Should().Be(2, "Segundo cliente deve ter ID 2");
        id3.Should().Be(3, "Terceiro cliente deve ter ID 3");

        // Verificar no banco
        using var context = _databaseFixture.CreateDbContext();
        var todosClientes = await context.Clientes.OrderBy(c => c.ClienteId).ToListAsync();
        
        todosClientes.Should().HaveCount(3);
        todosClientes[0].Nome.Should().Be("Cliente 1");
        todosClientes[1].Nome.Should().Be("Cliente 2");
        todosClientes[2].Nome.Should().Be("Cliente 3");
    }
}

/// <summary>
/// Collection definition para compartilhar o TestDatabaseFixture entre testes.
/// Garante que o mesmo container seja usado para todos os testes desta collection.
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
    // Esta classe existe apenas para definir a collection fixture
}
