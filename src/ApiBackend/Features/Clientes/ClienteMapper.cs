using AutoMapper;
using ApiBackend.Features.Clientes.Models;
using ApiBackend.Features.Clientes.Dtos;
using ApiBackend.Features.Clientes.Dtos.Contatos;

namespace ApiBackend.Features.Clientes;

public class ClienteMapper : Profile
{
    public ClienteMapper()
    {
        CreateMap<NovoClienteDto, Cliente>();
        CreateMap<NovoContatoDto, Contato>();
    }
}
