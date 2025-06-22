using System.Text.Json;
using System.Text.Json.Serialization;
using ApiBackend.Features.Clientes.Dtos.Contatos;

namespace ApiBackend.Features.Clientes.Converters;

/// <summary>
/// Custom JSON converter to handle polymorphic deserialization of NovoContatoDto
/// Based on the "tipo" property in the JSON
/// </summary>
public class NovoContatoDtoConverter : JsonConverter<NovoContatoDto>
{
    public override NovoContatoDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (!root.TryGetProperty("tipo", out var tipoElement))
        {
            throw new JsonException("Propriedade 'tipo' é obrigatória para contatos");
        }

        if (!root.TryGetProperty("valor", out var valorElement))
        {
            throw new JsonException("Propriedade 'valor' é obrigatória para contatos");
        }

        var tipo = tipoElement.GetString();
        var valor = valorElement.GetString();

        if (string.IsNullOrEmpty(valor))
        {
            throw new JsonException("Valor do contato não pode ser vazio");
        }

        return tipo?.ToLowerInvariant() switch
        {
            "email" => new EmailContatoDto(valor),
            "telefone" => new TelefoneContatoDto(valor),
            _ => throw new JsonException($"Tipo de contato não suportado: {tipo}")
        };
    }

    public override void Write(Utf8JsonWriter writer, NovoContatoDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        switch (value)
        {
            case EmailContatoDto email:
                writer.WriteString("tipo", "Email");
                writer.WriteString("valor", email.Valor);
                break;
            case TelefoneContatoDto telefone:
                writer.WriteString("tipo", "Telefone");
                writer.WriteString("valor", telefone.Valor);
                break;
            default:
                throw new JsonException($"Tipo de contato não suportado: {value.GetType().Name}");
        }

        writer.WriteEndObject();
    }
}
