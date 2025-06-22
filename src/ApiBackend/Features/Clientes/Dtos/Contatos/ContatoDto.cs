using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiBackend.Features.Clientes.Dtos.Contatos;

public abstract record NovoContatoDto : IValidatableObject
{
    public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
}
