using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Features.Clientes.Models;

namespace ApiBackend.Tests.Infrastructure;

/// <summary>
/// Builder pattern para criar objetos de teste de forma consistente e legível.
/// Facilita a criação de dados de teste com valores padrão sensatos.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Builder para criar DTOs de novos clientes para testes.
    /// </summary>
    public class NovoClienteDtoBuilder
    {
        private string _nome = "Cliente Teste";
        private string _cep = "01310-100"; // CEP válido para teste (Av. Paulista)
        private string? _numero = "123";
        private string? _complemento = "Apt 45";
        private List<NovoContatoDto> _contatos = new()
        {
            new("Email", "cliente.teste@email.com"),
            new("Telefone", "(11) 99999-9999")
        };

        public NovoClienteDtoBuilder ComNome(string nome)
        {
            _nome = nome;
            return this;
        }

        public NovoClienteDtoBuilder ComCep(string cep)
        {
            _cep = cep;
            return this;
        }

        public NovoClienteDtoBuilder ComNumero(string? numero)
        {
            _numero = numero;
            return this;
        }

        public NovoClienteDtoBuilder ComComplemento(string? complemento)
        {
            _complemento = complemento;
            return this;
        }

        public NovoClienteDtoBuilder ComContatos(params NovoContatoDto[] contatos)
        {
            _contatos = contatos.ToList();
            return this;
        }

        public NovoClienteDtoBuilder SemContatos()
        {
            _contatos = new List<NovoContatoDto>();
            return this;
        }

        public NovoClienteDtoBuilder AdicionarContato(string tipo, string valor)
        {
            _contatos.Add(new NovoContatoDto(tipo, valor));
            return this;
        }

        public NovoClienteDto Build()
        {
            return new NovoClienteDto(_nome, _cep, _numero, _complemento, _contatos);
        }
    }

    /// <summary>
    /// Builder para criar entidades Cliente para testes diretos no banco.
    /// </summary>
    public class ClienteBuilder
    {
        private string _nome = "Cliente Teste";
        private DateTime _dataCadastro = DateTime.UtcNow;
        private List<Endereco> _enderecos = new();
        private List<Contato> _contatos = new();

        public ClienteBuilder ComNome(string nome)
        {
            _nome = nome;
            return this;
        }

        public ClienteBuilder ComDataCadastro(DateTime data)
        {
            _dataCadastro = data;
            return this;
        }

        public ClienteBuilder ComEndereco(string cep = "01310-100", string logradouro = "Avenida Paulista", 
            string bairro = "Bela Vista", string cidade = "São Paulo", string estado = "SP", 
            string numero = "123")
        {
            var endereco = new Endereco
            {
                Cep = cep,
                Logradouro = logradouro,
                Bairro = bairro,
                Cidade = cidade,
                Estado = estado,
                Numero = numero
            };
            _enderecos.Add(endereco);
            return this;
        }

        public ClienteBuilder ComContato(string tipo = "Email", string valor = "teste@email.com")
        {
            var contato = new Contato
            {
                Tipo = tipo,
                Valor = valor
            };
            _contatos.Add(contato);
            return this;
        }

        public Cliente Build()
        {
            var cliente = new Cliente
            {
                Nome = _nome,
                DataCadastroUtc = _dataCadastro,
                Enderecos = _enderecos,
                Contatos = _contatos
            };

            // Configura as relações
            foreach (var endereco in _enderecos)
                endereco.Cliente = cliente;
            
            foreach (var contato in _contatos)
                contato.Cliente = cliente;

            return cliente;
        }
    }

    /// <summary>
    /// Cria um builder para NovoClienteDto.
    /// </summary>
    public static NovoClienteDtoBuilder NovoCliente() => new();

    /// <summary>
    /// Cria um builder para entidade Cliente.
    /// </summary>
    public static ClienteBuilder Cliente() => new();

    /// <summary>
    /// Cria um DTO de resposta do ViaCEP para testes.
    /// </summary>
    public static EnderecoCepDto CriarEnderecoCepDto(
        string cep = "01310-100", 
        string logradouro = "Avenida Paulista",
        string bairro = "Bela Vista", 
        string localidade = "São Paulo", 
        string uf = "SP")
    {
        return new EnderecoCepDto(
            cep,
            logradouro,
            null, // complemento
            bairro,
            localidade,
            uf,
            null, // unidade
            null, // ibge
            null  // gia
        );
    }

    /// <summary>
    /// Cria uma lista de clientes para testes de listagem.
    /// </summary>
    public static List<Cliente> CriarListaClientes(int quantidade = 3)
    {
        var clientes = new List<Cliente>();
        
        for (int i = 1; i <= quantidade; i++)
        {
            var cliente = Cliente()
                .ComNome($"Cliente {i}")
                .ComEndereco(numero: i.ToString())
                .ComContato("Email", $"cliente{i}@teste.com")
                .ComContato("Telefone", $"(11) 9999{i:D4}-{i:D4}")
                .Build();
                
            clientes.Add(cliente);
        }
        
        return clientes;
    }
}
