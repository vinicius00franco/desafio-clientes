using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ApiBackend.Features.Clientes.Dtos;

namespace ApiBackend.Features.Clientes.Dtos;

public record NovoClienteDto(
    [property: Required(ErrorMessage = "Nome é obrigatório.")]
    [property: StringLength(100, ErrorMessage = "Nome deve ter no máximo 100 caracteres.")]
    string Nome,

    [property: Required(ErrorMessage = "CEP é obrigatório.")]
    [property: RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "CEP inválido. Formato esperado: 00000-000 ou 00000000.")]
    string Cep,

    string? Numero,

    string? Complemento,

    [property: Required(ErrorMessage = "Informe ao menos um contato.")]
    [property: MinLength(1, ErrorMessage = "É necessário pelo menos um contato.")]
    IReadOnlyCollection<NovoContatoDto> Contatos
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Exemplo de validação cruzada: proibir CEP específico
        if (Cep == "00000-000")
        {
            yield return new ValidationResult(
                "CEP não pode ser 00000-000",
                new[] { nameof(Cep) }
            );
        }
    }
}
