using ApiBackend.Features.Clientes.Models;
using ApiBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiBackend.Features.Clientes.Repositories;

public class ClienteRepository
{
    private readonly ContextoApp _context;

    public ClienteRepository(ContextoApp context)
    {
        _context = context;
    }

    public async Task<int> Adicionar(Cliente cliente)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync();
        return cliente.ClienteId;
    }

    public async Task<Cliente?> ObterPorId(int id)
    {
        return await _context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == id);
    }

    public async Task<IEnumerable<Cliente>> ListarTodos()
    {
        return await _context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .ToListAsync();
    }

    public async Task Atualizar(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
        await _context.SaveChangesAsync();
    }

    public async Task Remover(int id)
    {
        var cliente = await _context.Clientes
            .Include(c => c.Enderecos)
            .Include(c => c.Contatos)
            .FirstOrDefaultAsync(c => c.ClienteId == id);

        if (cliente != null)
        {
            // A geração de histórico será feita automaticamente pelo banco de dados
            // através de triggers quando a operação de remoção for executada
            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> Existe(int id)
    {
        return await _context.Clientes.AnyAsync(c => c.ClienteId == id);
    }
}
