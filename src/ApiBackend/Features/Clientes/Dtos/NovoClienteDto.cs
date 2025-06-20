namespace ApiBackend.Features.Clientes.Dtos;

public record NovoClienteDto(
    string Nome,
    string Cep,
    string? Numero,
    string? Complemento,
    IReadOnlyCollection<NovoContatoDto> Contatos);
