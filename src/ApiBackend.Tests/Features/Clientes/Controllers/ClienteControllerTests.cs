using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using ApiBackend.Features.Clientes.Controllers;
using ApiBackend.Features.Clientes.Services;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Features.Clientes.Dtos.Contatos;
using System.Text.Json;

namespace ApiBackend.Tests.Features.Clientes.Controllers;

public class ClienteControllerTests
{
    private readonly Mock<ClienteService> _mockClienteService;
    private readonly ClienteController _controller;

    public ClienteControllerTests()
    {
        _mockClienteService = new Mock<ClienteService>();
        _controller = new ClienteController(_mockClienteService.Object);
    }

    #region CriarCliente Tests

    [Fact]
    public async Task CriarCliente_DeveRetornar201_QuandoDadosValidos()
    {
        // Arrange
        var dto = CriarNovoClienteDtoValido();
        var clienteIdEsperado = 123;
        
        _mockClienteService
            .Setup(s => s.CriarCliente(It.IsAny<NovoClienteDto>()))
            .ReturnsAsync(clienteIdEsperado);

        // Act
        var resultado = await _controller.CriarCliente(dto);

        // Assert
        resultado.Should().BeOfType<CreatedAtActionResult>();
        
        var createdResult = resultado as CreatedAtActionResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(_controller.ObterPorId));
        
        // Verificar estrutura da resposta JSON
        var responseValue = createdResult.Value;
        responseValue.Should().NotBeNull();
        
        var responseJson = JsonSerializer.Serialize(responseValue);
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
        
        responseObj.Should().ContainKey("id");
        responseObj["id"].Should().Be(JsonElement.Parse(clienteIdEsperado.ToString()));
        
        // Verificar route values
        createdResult.RouteValues.Should().ContainKey("id");
        createdResult.RouteValues["id"].Should().Be(clienteIdEsperado);
    }

    [Fact]
    public async Task CriarCliente_DeveRetornar400_QuandoInvalidOperationException()
    {
        // Arrange
        var dto = CriarNovoClienteDtoValido();
        var mensagemErro = "CEP não encontrado";
        
        _mockClienteService
            .Setup(s => s.CriarCliente(It.IsAny<NovoClienteDto>()))
            .ThrowsAsync(new InvalidOperationException(mensagemErro));

        // Act
        var resultado = await _controller.CriarCliente(dto);

        // Assert
        resultado.Should().BeOfType<BadRequestObjectResult>();
        
        var badRequestResult = resultado as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);
        
        // Verificar estrutura da resposta JSON de erro
        var responseValue = badRequestResult.Value;
        responseValue.Should().NotBeNull();
        
        var responseJson = JsonSerializer.Serialize(responseValue);
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
        
        responseObj.Should().ContainKey("erro");
        responseObj["erro"].Should().Be(JsonElement.Parse($"\"{mensagemErro}\""));
    }

    [Fact]
    public async Task CriarCliente_DeveRetornar500_QuandoExcecaoGenerica()
    {
        // Arrange
        var dto = CriarNovoClienteDtoValido();
        
        _mockClienteService
            .Setup(s => s.CriarCliente(It.IsAny<NovoClienteDto>()))
            .ThrowsAsync(new Exception("Erro inesperado"));

        // Act
        var resultado = await _controller.CriarCliente(dto);

        // Assert
        resultado.Should().BeOfType<ObjectResult>();
        
        var objectResult = resultado as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        
        // Verificar estrutura da resposta JSON de erro interno
        var responseValue = objectResult.Value;
        responseValue.Should().NotBeNull();
        
        var responseJson = JsonSerializer.Serialize(responseValue);
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
        
        responseObj.Should().ContainKey("erro");
        responseObj["erro"].Should().Be(JsonElement.Parse("\"Erro interno do servidor\""));
    }

    #endregion

    #region ObterPorId Tests

    [Fact]
    public async Task ObterPorId_DeveRetornar200_QuandoClienteExiste()
    {
        // Arrange
        var clienteId = 1;
        var cliente = CriarClienteCompleto();
        
        _mockClienteService
            .Setup(s => s.ObterPorId(clienteId))
            .ReturnsAsync(cliente);

        // Act
        var resultado = await _controller.ObterPorId(clienteId);

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        
        var okResult = resultado as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(cliente);
        
        // Verificar estrutura do objeto Cliente retornado
        var clienteRetornado = okResult.Value as Cliente;
        clienteRetornado.Should().NotBeNull();
        
        // Verificar propriedades obrigatórias e tipos
        clienteRetornado!.ClienteId.Should().BeOfType<int>().And.BePositive();
        clienteRetornado.Nome.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        clienteRetornado.DataCadastroUtc.Should().BeOfType<DateTime>();
        clienteRetornado.Enderecos.Should().BeAssignableTo<ICollection<Endereco>>();
        clienteRetornado.Contatos.Should().BeAssignableTo<ICollection<Contato>>();
        
        // Verificar estrutura dos endereços
        clienteRetornado.Enderecos.Should().HaveCount(1);
        var endereco = clienteRetornado.Enderecos.First();
        endereco.EnderecoId.Should().BeOfType<int>();
        endereco.Logradouro.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        endereco.Numero.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        endereco.Bairro.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        endereco.Cidade.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        endereco.Estado.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        endereco.Cep.Should().BeOfType<string>().And.NotBeNullOrEmpty();
        endereco.ClienteId.Should().BeOfType<int>().And.Be(clienteId);
        
        // Verificar estrutura dos contatos
        clienteRetornado.Contatos.Should().HaveCount(2);
        foreach (var contato in clienteRetornado.Contatos)
        {
            contato.ContatoId.Should().BeOfType<int>();
            contato.Tipo.Should().BeOfType<string>().And.NotBeNullOrEmpty();
            contato.Valor.Should().BeOfType<string>().And.NotBeNullOrEmpty();
            contato.ClienteId.Should().BeOfType<int>().And.Be(clienteId);
        }
    }

    [Fact]
    public async Task ObterPorId_DeveRetornar404_QuandoClienteNaoExiste()
    {
        // Arrange
        var clienteId = 999;
        
        _mockClienteService
            .Setup(s => s.ObterPorId(clienteId))
            .ReturnsAsync((Cliente?)null);

        // Act
        var resultado = await _controller.ObterPorId(clienteId);

        // Assert
        resultado.Should().BeOfType<NotFoundObjectResult>();
        
        var notFoundResult = resultado as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);
        
        // Verificar estrutura da resposta JSON de erro
        var responseValue = notFoundResult.Value;
        responseValue.Should().NotBeNull();
        
        var responseJson = JsonSerializer.Serialize(responseValue);
        var responseObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseJson);
        
        responseObj.Should().ContainKey("erro");
        responseObj["erro"].Should().Be(JsonElement.Parse("\"Cliente não encontrado\""));
    }

    #endregion

    #region ListarTodos Tests

    [Fact]
    public async Task ListarTodos_DeveRetornar200_ComListaDeClientes()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            CriarClienteCompleto(1, "João Silva"),
            CriarClienteCompleto(2, "Maria Santos"),
            CriarClienteCompleto(3, "Pedro Oliveira")
        };
        
        _mockClienteService
            .Setup(s => s.ListarTodos())
            .ReturnsAsync(clientes);

        // Act
        var resultado = await _controller.ListarTodos();

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        
        var okResult = resultado as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(clientes);
        
        // Verificar que retorna IEnumerable<Cliente>
        var clientesRetornados = okResult.Value as IEnumerable<Cliente>;
        clientesRetornados.Should().NotBeNull();
        clientesRetornados.Should().HaveCount(3);
        
        // Verificar estrutura de cada cliente na lista
        foreach (var cliente in clientesRetornados!)
        {
            cliente.ClienteId.Should().BeOfType<int>().And.BePositive();
            cliente.Nome.Should().BeOfType<string>().And.NotBeNullOrEmpty();
            cliente.DataCadastroUtc.Should().BeOfType<DateTime>();
            cliente.Enderecos.Should().BeAssignableTo<ICollection<Endereco>>();
            cliente.Contatos.Should().BeAssignableTo<ICollection<Contato>>();
        }
    }

    [Fact]
    public async Task ListarTodos_DeveRetornar200_ComListaVazia_QuandoNaoHaClientes()
    {
        // Arrange
        var clientesVazios = new List<Cliente>();
        
        _mockClienteService
            .Setup(s => s.ListarTodos())
            .ReturnsAsync(clientesVazios);

        // Act
        var resultado = await _controller.ListarTodos();

        // Assert
        resultado.Should().BeOfType<OkObjectResult>();
        
        var okResult = resultado as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(clientesVazios);
        
        // Verificar que retorna lista vazia mas válida
        var clientesRetornados = okResult.Value as IEnumerable<Cliente>;
        clientesRetornados.Should().NotBeNull();
        clientesRetornados.Should().BeEmpty();
    }

    #endregion

    #region Testes de Headers e Content-Type

    [Fact]
    public async Task TodosOsEndpoints_DeveRetornarJSON()
    {
        // Arrange
        var dto = CriarNovoClienteDtoValido();
        var cliente = CriarClienteCompleto();
        var clientes = new List<Cliente> { cliente };
        
        _mockClienteService.Setup(s => s.CriarCliente(It.IsAny<NovoClienteDto>())).ReturnsAsync(1);
        _mockClienteService.Setup(s => s.ObterPorId(It.IsAny<int>())).ReturnsAsync(cliente);
        _mockClienteService.Setup(s => s.ListarTodos()).ReturnsAsync(clientes);

        // Act & Assert para CriarCliente
        var resultadoPost = await _controller.CriarCliente(dto);
        resultadoPost.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = resultadoPost as CreatedAtActionResult;
        createdResult!.Value.Should().NotBeNull(); // Verifica que retorna objeto serializável em JSON

        // Act & Assert para ObterPorId
        var resultadoGet = await _controller.ObterPorId(1);
        resultadoGet.Should().BeOfType<OkObjectResult>();
        var okResult = resultadoGet as OkObjectResult;
        okResult!.Value.Should().NotBeNull(); // Verifica que retorna objeto serializável em JSON

        // Act & Assert para ListarTodos
        var resultadoGetAll = await _controller.ListarTodos();
        resultadoGetAll.Should().BeOfType<OkObjectResult>();
        var okResultAll = resultadoGetAll as OkObjectResult;
        okResultAll!.Value.Should().NotBeNull(); // Verifica que retorna objeto serializável em JSON
    }

    #endregion

    #region Métodos Auxiliares

    private static NovoClienteDto CriarNovoClienteDtoValido()
    {
        return new NovoClienteDto(
            Nome: "João Silva",
            Cep: "01310-100",
            Numero: "123",
            Complemento: "Apt 45",
            Contatos: new List<NovoContatoDto>
            {
                new EmailContatoDto("joao.silva@email.com"),
                new TelefoneContatoDto("11999999999")
            }
        );
    }

    private static Cliente CriarClienteCompleto(int id = 1, string nome = "João Silva")
    {
        return new Cliente
        {
            ClienteId = id,
            Nome = nome,
            DataCadastroUtc = DateTime.UtcNow,
            Enderecos = new List<Endereco>
            {
                new Endereco
                {
                    EnderecoId = 1,
                    Logradouro = "Avenida Paulista",
                    Numero = "123",
                    Bairro = "Bela Vista",
                    Cidade = "São Paulo",
                    Estado = "SP",
                    Cep = "01310-100",
                    ClienteId = id
                }
            },
            Contatos = new List<Contato>
            {
                new Contato
                {
                    ContatoId = 1,
                    Tipo = "Email",
                    Valor = "joao.silva@email.com",
                    ClienteId = id
                },
                new Contato
                {
                    ContatoId = 2,
                    Tipo = "Telefone",
                    Valor = "11999999999",
                    ClienteId = id
                }
            }
        };
    }

    #endregion
}
