using FixTintas.Repositorio.Contexto;
using Microsoft.EntityFrameworkCore;

namespace FixTintas.Repositorio;

// ============================================================================
// Lançada quando o BANCO recusa uma gravação. Acontece em quatro casos:
//   - índice único: e-mail, CPF, SKU, código de cupom... repetido;
//   - regra CHECK: por exemplo, o estoque ficaria negativo (CK_Produtos_Estoque);
//   - chave estrangeira: tentar excluir algo que ainda está em uso;
//   - alteração simultânea: outra pessoa gravou o mesmo registro (RowVersion).
//
// O Negocio já confere quase tudo antes de gravar; esta exceção cobre o caso raro
// de duas pessoas fazerem a mesma coisa no mesmo instante. O Negocio troca por
// uma mensagem amigável (ConflitoException).
// ============================================================================
public class ConflitoDeGravacaoException : Exception
{
    public ConflitoDeGravacaoException(Exception causa)
        : base("O banco de dados recusou a gravação: " + (causa.InnerException?.Message ?? causa.Message), causa) { }

    // true = outra pessoa alterou o mesmo registro ao mesmo tempo.
    public bool Concorrencia => InnerException is DbUpdateConcurrencyException;
}

// Atalho usado por todos os repositórios no lugar de SaveChanges().
internal static class Gravacao
{
    public static void Gravar(this AppDbContext contexto)
    {
        try
        {
            contexto.SaveChanges();
        }
        catch (DbUpdateException ex)
        {
            // Esquece as alterações que falharam, para o contexto continuar usável
            // nesta mesma requisição (por exemplo, para mostrar a tela de novo).
            contexto.ChangeTracker.Clear();
            throw new ConflitoDeGravacaoException(ex);
        }
    }
}
