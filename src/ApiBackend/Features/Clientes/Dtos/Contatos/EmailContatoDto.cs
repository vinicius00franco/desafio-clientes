using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiBackend.Features.Clientes.Dtos.Contatos;

public record EmailContatoDto(
    [property: Required(ErrorMessage = "Valor do contato é obrigatório.")]
    [property: EmailAddress(ErrorMessage = "Email inválido.")]
    string Valor
) : NovoContatoDto
{
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // DataAnnotations already applied for EmailAddress
        yield break;
    }
}
