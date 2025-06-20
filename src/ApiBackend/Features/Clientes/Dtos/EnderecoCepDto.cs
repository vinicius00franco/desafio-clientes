namespace ApiBackend.Features.Clientes.Dtos;

public record EnderecoCepDto(
    string Cep,
    string Logradouro,
    string? Complemento,
    string Bairro,
    string Localidade,
    string Uf,
    string? Unidade,
    string? Ibge,
    string? Gia);
