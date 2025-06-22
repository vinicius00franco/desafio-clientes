using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApiBackend.Features.Clientes.Dtos;

public abstract record NovoContatoDto : IValidatableObject
{
    public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);
}
