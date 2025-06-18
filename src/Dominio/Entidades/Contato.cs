using System;

namespace Dominio.Entidades;

public class Contato
{
    public int ContatoId { get; set; }
    public string Tipo { get; set; } = null!;
    public string Valor { get; set; } = null!;
    public int ClienteId { get; set; }
    public Cliente Cliente { get; set; } = null!;
}
