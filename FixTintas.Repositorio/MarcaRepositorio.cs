using FixTintas.Modelos;
using FixTintas.Repositorio.Contexto;
using FixTintas.Repositorio.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// Única classe que conversa com a tabela Marcas.
public class MarcaRepositorio : IMarcaRepositorio
{
    private readonly AppDbContext _contexto;

    public MarcaRepositorio(AppDbContext contexto)
    {
        _contexto = contexto;
    }

    public List<Marca> Listar(bool incluirInativas) =>
        _contexto.Marcas.AsNoTracking()
            .Where(m => incluirInativas || !m.Inativa)
            .OrderBy(m => m.Nome)
            .ToList();

    public Marca? ObterPorId(int id) =>
        _contexto.Marcas.FirstOrDefault(m => m.Id == id);

    public bool NomeEmUso(string nome, int? ignorarId = null) =>
        _contexto.Marcas.Any(m => m.Nome == nome && m.Id != ignorarId);

    public bool PossuiProdutos(int id) =>
        _contexto.Produtos.Any(p => p.MarcaId == id);

    public void Adicionar(Marca marca)
    {
        _contexto.Marcas.Add(marca);
        _contexto.Gravar();
    }

    public void Atualizar(Marca marca)
    {
        if (_contexto.Entry(marca).State == EntityState.Detached)
            _contexto.Marcas.Update(marca);
        _contexto.Gravar();
    }

    public void Remover(Marca marca)
    {
        _contexto.Marcas.Remove(marca);
        _contexto.Gravar();
    }
}
