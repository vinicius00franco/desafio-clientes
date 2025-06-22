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
        
        // Base mapping for NovoContatoDto to Contato
        CreateMap<NovoContatoDto, Contato>()
            .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => GetContatoTipo(src)))
            .ForMember(dest => dest.Valor, opt => opt.MapFrom(src => GetContatoValor(src)));
            
        // Specific mappings for derived types
        CreateMap<EmailContatoDto, Contato>()
            .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => "Email"))
            .ForMember(dest => dest.Valor, opt => opt.MapFrom(src => src.Valor));
            
        CreateMap<TelefoneContatoDto, Contato>()
            .ForMember(dest => dest.Tipo, opt => opt.MapFrom(src => "Telefone"))
            .ForMember(dest => dest.Valor, opt => opt.MapFrom(src => src.Valor));
    }
    
    private static string GetContatoTipo(NovoContatoDto contatoDto)
    {
        return contatoDto switch
        {
            EmailContatoDto => "Email",
            TelefoneContatoDto => "Telefone",
            _ => throw new NotSupportedException($"Tipo de contato não suportado: {contatoDto.GetType().Name}")
        };
    }
    
    private static string GetContatoValor(NovoContatoDto contatoDto)
    {
        return contatoDto switch
        {
            EmailContatoDto email => email.Valor,
            TelefoneContatoDto telefone => telefone.Valor,
            _ => throw new NotSupportedException($"Tipo de contato não suportado: {contatoDto.GetType().Name}")
        };
    }
}
