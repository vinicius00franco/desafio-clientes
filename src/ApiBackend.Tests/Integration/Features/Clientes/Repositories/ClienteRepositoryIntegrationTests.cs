using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Repositories;
using ApiBackend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ApiBackend.Tests.Integration.Features.Clientes.Repositories;

/// <summary>
/// Testes de integração para ClienteRepository usando Testcontainers com SQL Server real.
/// Foca especificamente nas operações de persistência e consulta no banco de dados:
/// - CRUD operations
/// - Relacionamentos e eager loading
/// - Queries complexas
/// - Constraints e validações do banco
/// </summary>
[Collection("Database")]
public class ClienteRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly TestDatabaseFixture _databaseFixture;
    private readonly ClienteRepository _repository;

    public ClienteRepositoryIntegrationTests(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
        
        using var serviceFactory = new TestServiceFactory(_databaseFixture);
        _repository = serviceFactory.GetService<ClienteRepository>();
    }

    public async Task InitializeAsync()
    {
        // Verifica saúde do banco e limpa dados
        var isHealthy = await _databaseFixture.IsHealthyAsync();
        isHealthy.Should().BeTrue("O banco de dados deve estar acessível");
        
        await _databaseFixture.CleanDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Adicionar_ComClienteCompleto_DevePersistirComRelacionamentos()
    {
        // Arrange
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Pedro Silva")
            .ComEndereco("12345-678", "Rua A", "Bairro B", "Cidade C", "SP", "100")
            .ComContato("Email", "pedro@teste.com")
            .ComContato("Telefone", "(11) 99999-8888")
            .Build();

        // Act
        var clienteId = await _repository.Adicionar(cliente);

        // Assert
        clienteId.Should().BeGreaterThan(0, "ID deve ser gerado automaticamente");
        cliente.ClienteId.Should().Be(clienteId, "Entidade deve ter o ID atualizado");

        // Verificar persistência direta no banco
        using var context = _databaseFixture.CreateDbContext();
        
        var clienteSalvo = await context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

        clienteSalvo.Should().NotBeNull();
        clienteSalvo!.Nome.Should().Be("Pedro Silva");
        clienteSalvo.Enderecos.Should().HaveCount(1);
        clienteSalvo.Contatos.Should().HaveCount(2);

        // Verificar FK's
        var endereco = clienteSalvo.Enderecos.First();
        endereco.ClienteId.Should().Be(clienteId);
        
        foreach (var contato in clienteSalvo.Contatos)
        {
            contato.ClienteId.Should().Be(clienteId);
        }
    }

    [Fact]
    public async Task Adicionar_ComClienteSemRelacionamentos_DevePersistirApenasCliente()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "Cliente Simples",
            DataCadastroUtc = DateTime.UtcNow
        };

        // Act
        var clienteId = await _repository.Adicionar(cliente);

        // Assert
        clienteId.Should().BeGreaterThan(0);

        using var context = _databaseFixture.CreateDbContext();
        var clienteSalvo = await context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

        clienteSalvo.Should().NotBeNull();
        clienteSalvo!.Nome.Should().Be("Cliente Simples");
        clienteSalvo.Enderecos.Should().BeEmpty();
        clienteSalvo.Contatos.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterPorId_ComIdExistente_DeveRetornarClienteComRelacionamentos()
    {
        // Arrange - Criar cliente diretamente no banco
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Ana Costa")
            .ComEndereco("87654-321", "Avenida B", "Centro", "Rio de Janeiro", "RJ", "200")
            .ComContato("Email", "ana@teste.com")
            .ComContato("WhatsApp", "(21) 98888-7777")
            .ComContato("Telefone", "(21) 3333-4444")
            .Build();

        int clienteId;
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        // Act
        var clienteEncontrado = await _repository.ObterPorId(clienteId);

        // Assert
        clienteEncontrado.Should().NotBeNull();
        clienteEncontrado!.ClienteId.Should().Be(clienteId);
        clienteEncontrado.Nome.Should().Be("Ana Costa");

        // Verificar eager loading dos relacionamentos
        clienteEncontrado.Enderecos.Should().HaveCount(1);
        var endereco = clienteEncontrado.Enderecos.First();
        endereco.Cep.Should().Be("87654-321");
        endereco.Logradouro.Should().Be("Avenida B");
        endereco.Cidade.Should().Be("Rio de Janeiro");

        clienteEncontrado.Contatos.Should().HaveCount(3);
        clienteEncontrado.Contatos.Should().Contain(c => c.Tipo == "Email" && c.Valor == "ana@teste.com");
        clienteEncontrado.Contatos.Should().Contain(c => c.Tipo == "WhatsApp" && c.Valor == "(21) 98888-7777");
        clienteEncontrado.Contatos.Should().Contain(c => c.Tipo == "Telefone" && c.Valor == "(21) 3333-4444");
    }

    [Fact]
    public async Task ObterPorId_ComIdInexistente_DeveRetornarNull()
    {
        // Arrange
        const int idInexistente = 999999;

        // Act
        var cliente = await _repository.ObterPorId(idInexistente);

        // Assert
        cliente.Should().BeNull();
    }

    [Fact]
    public async Task ListarTodos_ComMultiplosClientes_DeveRetornarTodosComRelacionamentos()
    {
        // Arrange - Criar múltiplos clientes
        var clientes = new List<Cliente>
        {
            TestDataBuilder.Cliente()
                .ComNome("Cliente A")
                .ComEndereco("11111-111", "Rua 1", "Bairro 1", "Cidade 1", "SP")
                .ComContato("Email", "a@teste.com")
                .Build(),
                
            TestDataBuilder.Cliente()
                .ComNome("Cliente B")
                .ComEndereco("22222-222", "Rua 2", "Bairro 2", "Cidade 2", "RJ")
                .ComContato("Telefone", "(11) 1111-1111")
                .ComContato("Email", "b@teste.com")
                .Build(),
                
            TestDataBuilder.Cliente()
                .ComNome("Cliente C")
                .ComEndereco("33333-333", "Rua 3", "Bairro 3", "Cidade 3", "MG")
                .ComContato("WhatsApp", "(31) 2222-2222")
                .Build()
        };

        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.AddRange(clientes);
            await context.SaveChangesAsync();
        }

        // Act
        var clientesRetornados = await _repository.ListarTodos();

        // Assert
        var lista = clientesRetornados.ToList();
        lista.Should().HaveCount(3);

        // Verificar se todos têm relacionamentos carregados
        foreach (var cliente in lista)
        {
            cliente.Enderecos.Should().NotBeEmpty("Todos os clientes de teste têm endereços");
            cliente.Contatos.Should().NotBeEmpty("Todos os clientes de teste têm contatos");
        }

        // Verificar clientes específicos
        lista.Should().Contain(c => c.Nome == "Cliente A");
        lista.Should().Contain(c => c.Nome == "Cliente B");
        lista.Should().Contain(c => c.Nome == "Cliente C");

        // Verificar diferentes quantidades de contatos
        var clienteB = lista.First(c => c.Nome == "Cliente B");
        clienteB.Contatos.Should().HaveCount(2, "Cliente B tem 2 contatos");
    }

    [Fact]
    public async Task ListarTodos_SemClientes_DeveRetornarListaVazia()
    {
        // Act
        var clientes = await _repository.ListarTodos();

        // Assert
        clientes.Should().BeEmpty();
    }

    [Fact]
    public async Task Atualizar_ComClienteExistente_DeveAtualizarNoBanco()
    {
        // Arrange - Criar cliente inicial
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Nome Original")
            .ComEndereco()
            .ComContato("Email", "original@teste.com")
            .Build();

        int clienteId;
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        // Buscar cliente para atualização
        var clienteParaAtualizar = await _repository.ObterPorId(clienteId);
        clienteParaAtualizar.Should().NotBeNull();

        // Modificar dados
        clienteParaAtualizar!.Nome = "Nome Atualizado";
        clienteParaAtualizar.Contatos.First().Valor = "atualizado@teste.com";

        // Act
        await _repository.Atualizar(clienteParaAtualizar);

        // Assert - Verificar mudanças no banco
        using var contextVerificacao = _databaseFixture.CreateDbContext();
        var clienteAtualizado = await contextVerificacao.Clientes
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);

        clienteAtualizado.Should().NotBeNull();
        clienteAtualizado!.Nome.Should().Be("Nome Atualizado");
        clienteAtualizado.Contatos.First().Valor.Should().Be("atualizado@teste.com");
    }

    [Fact]
    public async Task Remover_ComClienteExistente_DeveRemoverDobancoComRelacionamentos()
    {
        // Arrange - Criar cliente com relacionamentos
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Cliente Para Remover")
            .ComEndereco("99999-999", "Rua Final", "Ultimo Bairro", "Cidade End", "SP")
            .ComContato("Email", "remover@teste.com")
            .ComContato("Telefone", "(11) 0000-0000")
            .Build();

        int clienteId;
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        // Verificar que existe
        var clienteExistente = await _repository.ObterPorId(clienteId);
        clienteExistente.Should().NotBeNull();

        // Act
        await _repository.Remover(clienteId);

        // Assert - Verificar remoção completa
        var clienteRemovido = await _repository.ObterPorId(clienteId);
        clienteRemovido.Should().BeNull("Cliente deve ter sido removido");

        // Verificar remoção em cascata dos relacionamentos
        using var contextVerificacaoRemocao = _databaseFixture.CreateDbContext();
        
        var enderecosRemovidos = await contextVerificacaoRemocao.Enderecos
            .Where(e => e.ClienteId == clienteId)
            .CountAsync();
        enderecosRemovidos.Should().Be(0, "Endereços devem ser removidos em cascata");

        var contatosRemovidos = await contextVerificacaoRemocao.Contatos
            .Where(c => c.ClienteId == clienteId)
            .CountAsync();
        contatosRemovidos.Should().Be(0, "Contatos devem ser removidos em cascata");
    }

    [Fact]
    public async Task Remover_ComIdInexistente_NaoDeveLancarExcecao()
    {
        // Arrange
        const int idInexistente = 999999;

        // Act & Assert - Não deve lançar exceção
        await _repository.Remover(idInexistente);

        // Operação deve ser idempotente
        await _repository.Remover(idInexistente);
    }

    [Fact]
    public async Task Existe_ComClienteExistente_DeveRetornarTrue()
    {
        // Arrange
        var cliente = TestDataBuilder.Cliente()
            .ComNome("Cliente Existente")
            .Build();

        int clienteId;
        using (var context = _databaseFixture.CreateDbContext())
        {
            context.Clientes.Add(cliente);
            await context.SaveChangesAsync();
            clienteId = cliente.ClienteId;
        }

        // Act
        var existe = await _repository.Existe(clienteId);

        // Assert
        existe.Should().BeTrue();
    }

    [Fact]
    public async Task Existe_ComClienteInexistente_DeveRetornarFalse()
    {
        // Arrange
        const int idInexistente = 999999;

        // Act
        var existe = await _repository.Existe(idInexistente);

        // Assert
        existe.Should().BeFalse();
    }

    [Fact]
    public async Task Operacoes_ComTransacao_DevemMantterConsistencia()
    {
        // Arrange
        var cliente1 = TestDataBuilder.Cliente().ComNome("Cliente 1").Build();
        var cliente2 = TestDataBuilder.Cliente().ComNome("Cliente 2").Build();

        // Act - Múltiplas operações
        var id1 = await _repository.Adicionar(cliente1);
        var id2 = await _repository.Adicionar(cliente2);

        // Atualizar o primeiro
        var clienteParaAtualizar = await _repository.ObterPorId(id1);
        clienteParaAtualizar!.Nome = "Cliente 1 Atualizado";
        await _repository.Atualizar(clienteParaAtualizar);

        // Remover o segundo
        await _repository.Remover(id2);

        // Assert - Verificar estado final
        var todosClientes = await _repository.ListarTodos();
        var lista = todosClientes.ToList();
        
        lista.Should().HaveCount(1, "Apenas um cliente deve restar");
        lista.First().Nome.Should().Be("Cliente 1 Atualizado");
        lista.First().ClienteId.Should().Be(id1);

        var cliente2Removido = await _repository.ObterPorId(id2);
        cliente2Removido.Should().BeNull("Cliente 2 deve ter sido removido");
    }
}
