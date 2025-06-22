using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ApiBackend.Features.Clientes.Dtos;

public record TelefoneContatoDto(
    [property: Required(ErrorMessage = "Valor do contato é obrigatório.")]
    [property: RegularExpression("^\\d{10,11}$", ErrorMessage = "Telefone inválido. Deve conter apenas dígitos e ter 10 ou 11 caracteres.")]
    string Valor
) : NovoContatoDto
{
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // DataAnnotations já valida o padrão
        yield break;
    }
}
