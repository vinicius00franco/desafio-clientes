using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Repositories;
using ApiBackend.Services.External;
using AutoMapper;

namespace ApiBackend.Features.Clientes.Services;

public class ClienteService
{
    private readonly ClienteRepository _clienteRepository;
    private readonly IViaCepService _viaCepService;
    private readonly IMapper _mapper;

    public ClienteService(ClienteRepository clienteRepository, IViaCepService viaCepService, IMapper mapper)
    {
        _clienteRepository = clienteRepository;
        _viaCepService = viaCepService;
        _mapper = mapper;
    }

    public async Task<int> CriarCliente(NovoClienteDto dto)
    {
        // Validações de negócio
        if (string.IsNullOrWhiteSpace(dto.Nome))
            throw new ArgumentException("Nome é obrigatório");

        if (!dto.Contatos.Any())
            throw new ArgumentException("Pelo menos um contato é obrigatório");

        // Buscar informações do CEP (regra de negócio)
        var infoCep = await _viaCepService.ObterPorCep(dto.Cep);

        if (infoCep == null)
        {
            throw new InvalidOperationException($"CEP {dto.Cep} não encontrado.");
        }

        // Construir entidade com regras de negócio
        var cliente = ConstruirCliente(dto, infoCep);

        // Persistir via Repository
        return await _clienteRepository.Adicionar(cliente);
    }

    public async Task<Cliente?> ObterPorId(int id)
    {
        if (id <= 0)
            throw new ArgumentException("ID deve ser maior que zero");

        return await _clienteRepository.ObterPorId(id);
    }

    public async Task<IEnumerable<Cliente>> ListarTodos()
    {
        return await _clienteRepository.ListarTodos();
    }

    private Cliente ConstruirCliente(NovoClienteDto dto, EnderecoCepDto infoCep)
    {
        // Mapear DTO para entidade
        var cliente = _mapper.Map<Cliente>(dto);

        cliente.Contatos.Clear(); // Remover mapeamento automático de contatos

        // Adicionar endereço com informações do ViaCEP (regra de negócio)
        var endereco = new Endereco
        {
            Cep = infoCep.Cep,
            Logradouro = infoCep.Logradouro,
            Bairro = infoCep.Bairro,
            Cidade = infoCep.Localidade,
            Estado = infoCep.Uf,
            Numero = dto.Numero ?? "S/N",
            Cliente = cliente
        };

        cliente.Enderecos.Add(endereco);

        // Mapear contatos (regra de negócio)
        foreach (var contatoDto in dto.Contatos)
        {
            var contato = _mapper.Map<Contato>(contatoDto);
            contato.Cliente = cliente;
            cliente.Contatos.Add(contato);
        }

        return cliente;
    }
}
