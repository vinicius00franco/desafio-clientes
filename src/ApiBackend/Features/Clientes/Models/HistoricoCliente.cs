using System;

namespace ApiBackend.Features.Clientes.Models;

public class HistoricoCliente
{
    public int HistoricoId { get; set; }
    public int ClienteId { get; set; }
    public string Nome { get; set; } = null!;
    public DateTime DataCadastroUtc { get; set; }
    public DateTime DataAlteracaoUtc { get; set; } = DateTime.UtcNow;
    public string Operacao { get; set; } = null!;
}
