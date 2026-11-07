using System.Text.RegularExpressions;
using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// Regras dos cupons de desconto.
//   Percentual: Valor = porcentagem (10 = 10%), até 100.
//   ValorFixo:  Valor = reais (50 = R$ 50,00).
//   ValidoAte:  último dia em que o cupom vale (vazio = não vence).
//   UsosMaximos: quantas vezes pode ser usado no total (vazio = sem limite).
// Quem conta os usos é o sistema: +1 a cada pedido, -1 quando o pedido é cancelado.
// ============================================================================
public class CupomNegocio
{
    private static readonly Regex RegexCodigo = new("^[A-Z0-9_-]{3,40}$", RegexOptions.Compiled);

    private readonly ICupomRepositorio _repositorio;

    public CupomNegocio(ICupomRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public List<Cupom> Listar() => _repositorio.Listar();

    public Cupom? ObterPorId(int id) => _repositorio.ObterPorId(id);

    public Cupom Cadastrar(CupomSalvarDto dto)
    {
        var cupom = new Cupom();
        Preencher(cupom, dto);
        Validar(cupom, ignorarId: null);

        try
        {
            _repositorio.Adicionar(cupom);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe um cupom com este código.");
        }
        return cupom;
    }

    public void Atualizar(int id, CupomSalvarDto dto)
    {
        var cupom = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Cupom não encontrado.");

        Preencher(cupom, dto);
        Validar(cupom, ignorarId: id);

        try
        {
            _repositorio.Atualizar(cupom);
        }
        catch (ConflitoDeGravacaoException ex) when (ex.Concorrencia)
        {
            throw new ConflitoException("O cupom acabou de ser usado num pedido. Abra a tela de novo e repita a alteração.");
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Já existe um cupom com este código.");
        }
    }

    // Cupom que já foi usado em pedido não é apagado: é desativado.
    public ResultadoExclusao Remover(int id)
    {
        var cupom = _repositorio.ObterPorId(id)
            ?? throw new NaoEncontradoException("Cupom não encontrado.");

        if (_repositorio.PossuiPedidos(id))
        {
            cupom.Inativo = true;
            _repositorio.Atualizar(cupom);
            return ResultadoExclusao.Desativado;
        }

        _repositorio.Remover(cupom);
        return ResultadoExclusao.Excluido;
    }

    // "Percentual", "percentual", "ValorFixo", "valor fixo", "valor_fixo"... -> o enum.
    public static TipoDesconto? LerTipo(string? texto)
    {
        var limpo = Validacoes.RemoverAcentos((texto ?? string.Empty).Replace(" ", "").Replace("_", "").Trim());
        if (limpo.Equals("Percentual", StringComparison.OrdinalIgnoreCase)) return TipoDesconto.Percentual;
        if (limpo.Equals("ValorFixo", StringComparison.OrdinalIgnoreCase)) return TipoDesconto.ValorFixo;
        return null;
    }

    private static void Preencher(Cupom cupom, CupomSalvarDto dto)
    {
        cupom.Codigo = (dto.Codigo ?? string.Empty).Trim().ToUpperInvariant();
        cupom.Tipo = LerTipo(dto.Tipo)
            ?? throw new RegraDeNegocioException("Tipo de desconto inválido. Use \"Percentual\" ou \"ValorFixo\".");
        cupom.Valor = Precificador.Arredondar(dto.Valor);
        cupom.ValorMinimoPedido = Precificador.Arredondar(dto.ValorMinimoPedido);
        cupom.ValidoAte = dto.ValidoAte?.Date;
        cupom.UsosMaximos = dto.UsosMaximos;
        cupom.Inativo = dto.Inativo;
    }

    private void Validar(Cupom cupom, int? ignorarId)
    {
        if (!RegexCodigo.IsMatch(cupom.Codigo))
            throw new RegraDeNegocioException("O código deve ter de 3 a 40 letras, números, - ou _ (sem espaços).");

        if (cupom.Valor <= 0)
            throw new RegraDeNegocioException("O valor do desconto deve ser maior que zero.");

        if (cupom.Tipo == TipoDesconto.Percentual && cupom.Valor > 100)
            throw new RegraDeNegocioException("O desconto percentual pode ser de no máximo 100%.");

        if (cupom.ValorMinimoPedido < 0)
            throw new RegraDeNegocioException("O valor mínimo do pedido não pode ser negativo.");

        if (cupom.UsosMaximos is <= 0)
            throw new RegraDeNegocioException("O limite de usos deve ser 1 ou mais (ou deixe vazio para não ter limite).");

        if (_repositorio.CodigoEmUso(cupom.Codigo, ignorarId))
            throw new ConflitoException("Já existe um cupom com este código.");
    }
}
