namespace Dominio.Entidades;

public class Cliente
{
    public int ClienteId { get; set; }
    public string Nome { get; set; } = null!;
    public DateTime DataCadastroUtc { get; set; } = DateTime.UtcNow;
    public ICollection<Endereco> Enderecos { get; set; } = new List<Endereco>();
    public ICollection<Contato> Contatos { get; set; } = new List<Contato>();
}
