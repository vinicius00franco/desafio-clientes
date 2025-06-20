using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Services.External;
using Moq;
using Moq.Protected;
using Xunit;
using Xunit.Abstractions;

namespace ApiBackend.Tests.Services.External
{
    /// <summary>
    /// Testes para o serviço de integração com a API ViaCEP
    /// </summary>
    public class ViaCepServiceTests
    {
        private readonly ITestOutputHelper _saida;

        public ViaCepServiceTests(ITestOutputHelper saida)
        {
            _saida = saida;
        }

        #region Testes de Integração

        [Fact]
        public async Task ObterPorCep_QuandoCepValido_DeveRetornarEnderecoCompleto()
        {
            // Preparação
            var clienteHttp = new HttpClient();
            var servico = new ViaCepService(clienteHttp);
            var cep = "01310-100"; // Av. Paulista, São Paulo

            // Execução
            var resultado = await servico.ObterPorCep(cep);

            // Verificação
            Assert.NotNull(resultado);
            Assert.Equal("01310-100", resultado.Cep);
            Assert.Equal("Avenida Paulista", resultado.Logradouro);
            Assert.Equal("Bela Vista", resultado.Bairro);
            Assert.Equal("São Paulo", resultado.Localidade);
            Assert.Equal("SP", resultado.Uf);

            // Log para fins de depuração
            _saida.WriteLine($"CEP: {resultado.Cep}");
            _saida.WriteLine($"Logradouro: {resultado.Logradouro}");
            _saida.WriteLine($"Bairro: {resultado.Bairro}");
            _saida.WriteLine($"Cidade: {resultado.Localidade}");
            _saida.WriteLine($"Estado: {resultado.Uf}");
        }

        [Fact]
        public async Task ObterPorCep_QuandoCepInvalido_DeveRetornarNulo()
        {
            // Preparação
            var clienteHttp = new HttpClient();
            var servico = new ViaCepService(clienteHttp);
            var cep = "00000-000"; // CEP inválido

            // Execução
            var resultado = await servico.ObterPorCep(cep);

            // Verificação
            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObterPorCep_QuandoCepComFormatoAlternativo_DeveLimparFormatacaoERetornarEndereco()
        {
            // Preparação
            var clienteHttp = new HttpClient();
            var servico = new ViaCepService(clienteHttp);
            var cep = "01310.100"; // Formatado com ponto em vez de hífen

            // Execução
            var resultado = await servico.ObterPorCep(cep);

            // Verificação
            Assert.NotNull(resultado);
            Assert.Equal("01310-100", resultado.Cep); // Deve normalizar para formato padrão
            Assert.Equal("Avenida Paulista", resultado.Logradouro);
        }

        #endregion

        #region Testes Unitários com HTTP Simulado

        [Fact]
        public async Task ObterPorCep_QuandoCepValidoComRespostaSimulada_DeveRetornarDadosEsperados()
        {
            // Preparação
            var respostaSimulada = new EnderecoCepDto(
                Cep: "01310-100",
                Logradouro: "Avenida Paulista",
                Complemento: string.Empty,
                Bairro: "Bela Vista",
                Localidade: "São Paulo",
                Uf: "SP",
                Unidade: string.Empty,
                Ibge: "3550308",
                Gia: "1004"
            );

            var manipuladorSimulado = ConfigurarManipuladorSimulado("01310100", respostaSimulada, HttpStatusCode.OK);
            var clienteHttp = new HttpClient(manipuladorSimulado.Object);
            var servico = new ViaCepService(clienteHttp);

            // Execução
            var resultado = await servico.ObterPorCep("01310-100");

            // Verificação
            Assert.NotNull(resultado);
            Assert.Equal("01310-100", resultado.Cep);
            Assert.Equal("Avenida Paulista", resultado.Logradouro);
            Assert.Equal("Bela Vista", resultado.Bairro);
            Assert.Equal("São Paulo", resultado.Localidade);
            Assert.Equal("SP", resultado.Uf);
        }

        [Fact]
        public async Task ObterPorCep_QuandoCepVazio_DeveRetornarNulo()
        {
            // Preparação
            var manipuladorSimulado = new Mock<HttpMessageHandler>();
            var clienteHttp = new HttpClient(manipuladorSimulado.Object);
            var servico = new ViaCepService(clienteHttp);

            // Execução
            var resultado = await servico.ObterPorCep("");

            // Verificação
            Assert.Null(resultado);
            // Verifica que o cliente HTTP não foi chamado
            manipuladorSimulado.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task ObterPorCep_QuandoOcorreExcecaoHttp_DeveRetornarNulo()
        {
            // Preparação
            var manipuladorSimulado = new Mock<HttpMessageHandler>();
            manipuladorSimulado
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Erro de rede simulado"));

            var clienteHttp = new HttpClient(manipuladorSimulado.Object);
            var servico = new ViaCepService(clienteHttp);

            // Execução
            var resultado = await servico.ObterPorCep("01310-100");

            // Verificação
            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObterPorCep_QuandoServidorRetornaErro_DeveRetornarNulo()
        {
            // Preparação
            var manipuladorSimulado = ConfigurarManipuladorSimulado("01310100", new object(), HttpStatusCode.InternalServerError);
            var clienteHttp = new HttpClient(manipuladorSimulado.Object);
            var servico = new ViaCepService(clienteHttp);

            // Execução
            var resultado = await servico.ObterPorCep("01310-100");

            // Verificação
            Assert.Null(resultado);
        }

        [Fact]
        public async Task ObterPorCep_QuandoCepInexistente_DeveRetornarNulo()
        {
            // Preparação
            var respostaVazia = new EnderecoCepDto(
                Cep: "",
                Logradouro: "",
                Complemento: "",
                Bairro: "",
                Localidade: "",
                Uf: "",
                Unidade: "",
                Ibge: "",
                Gia: ""
            );

            var manipuladorSimulado = ConfigurarManipuladorSimulado("00000000", respostaVazia, HttpStatusCode.OK);
            var clienteHttp = new HttpClient(manipuladorSimulado.Object);
            var servico = new ViaCepService(clienteHttp);

            // Execução
            var resultado = await servico.ObterPorCep("00000-000");

            // Verificação
            Assert.Null(resultado);
        }

        #endregion

        #region Métodos Auxiliares

        private Mock<HttpMessageHandler> ConfigurarManipuladorSimulado<T>(string cep, T dadosResposta, HttpStatusCode codigoStatus)
        {
            var manipuladorSimulado = new Mock<HttpMessageHandler>();
            var resposta = new HttpResponseMessage
            {
                StatusCode = codigoStatus
            };

            if (dadosResposta != null)
            {
                resposta.Content = JsonContent.Create(dadosResposta);
            }

            manipuladorSimulado
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri != null && 
                        req.RequestUri.ToString().Contains(cep)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(resposta);

            return manipuladorSimulado;
        }

        #endregion
    }
}
