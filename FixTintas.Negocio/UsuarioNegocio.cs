using FixTintas.Modelos;
using FixTintas.Modelos.Dtos;
using FixTintas.Repositorio;
using FixTintas.Repositorio.Interfaces;

namespace FixTintas.Negocio;

// ============================================================================
// Regras de usuários: cadastro de cliente, login, dados da conta, troca de senha
// e a equipe da loja (funcionários e gerentes).
// A Web e a Api chamam esta classe; esta classe chama o repositório.
// ============================================================================
public class UsuarioNegocio
{
    private readonly IUsuarioRepositorio _repositorio;

    // Hash de uma senha qualquer, usado quando o e-mail do login não existe: o sistema
    // faz a mesma conta demorada e responde no mesmo tempo, então ninguém descobre
    // quais e-mails estão cadastrados medindo a demora da resposta.
    private static readonly string HashParaComparacaoFalsa = SenhaHasher.Gerar("senha-falsa-para-comparar-1");

    // INJEÇÃO DE DEPENDÊNCIA: pede o repositório no construtor; o ASP.NET entrega
    // (linha AddScoped<IUsuarioRepositorio, UsuarioRepositorio>() do Program.cs).
    public UsuarioNegocio(IUsuarioRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    // ------------------------------------------------------------------ login

    // Confere e-mail e senha. Devolve o usuário ou lança:
    //   CredenciaisInvalidasException -> "E-mail ou senha incorretos."
    //   AcessoNegadoException         -> "Conta desativada..."
    public Usuario Autenticar(string email, string senha)
    {
        var usuario = _repositorio.ObterPorEmail(Validacoes.NormalizarEmail(email));

        if (usuario is null)
        {
            SenhaHasher.Verificar(senha, HashParaComparacaoFalsa);
            throw new CredenciaisInvalidasException();
        }

        if (!SenhaHasher.Verificar(senha, usuario.SenhaHash))
            throw new CredenciaisInvalidasException();

        // Só avisa que a conta está desativada para quem acertou a senha.
        if (!usuario.Ativo)
            throw new AcessoNegadoException("Conta desativada. Procure o gerente da loja.");

        return usuario;
    }

    // Usuário ativo pelo id (usado a cada requisição para conferir o login).
    public Usuario? ObterAtivo(int id)
    {
        var usuario = _repositorio.ObterPorId(id);
        return usuario is { Ativo: true } ? usuario : null;
    }

    public Usuario ObterPorId(int id) =>
        _repositorio.ObterPorId(id) ?? throw new NaoEncontradoException("Usuário não encontrado.");

    public int ContarClientes() => _repositorio.ContarClientes();

    // --------------------------------------------------------------- cadastro

    // Cadastro de cliente. SEMPRE cria perfil "Cliente".
    //   exigirDadosPessoais = true  -> site: nome completo, CPF, nascimento (18+),
    //                                  telefone e confirmação da senha obrigatórios.
    //   exigirDadosPessoais = false -> app: só nome, e-mail e senha; o resto, se vier,
    //                                  é conferido do mesmo jeito.
    public Usuario CadastrarCliente(CadastroClienteDto dto, bool exigirDadosPessoais)
    {
        var nome = Validacoes.NormalizarTexto(dto.Nome);
        var email = Validacoes.NormalizarEmail(dto.Email);
        var cpf = Validacoes.SomenteDigitos(dto.Cpf);
        var telefone = Validacoes.SomenteDigitos(dto.Telefone);

        // Nome
        if (exigirDadosPessoais)
            ValidarNomeCompleto(nome, "Informe seu nome completo (nome e sobrenome).");
        else
            ValidarNome(nome);

        // E-mail
        if (!Validacoes.EmailValido(email))
            throw new RegraDeNegocioException("Informe um e-mail válido.");

        // Senha (e a confirmação, quando vier)
        ValidarSenha(dto.Senha);
        if (exigirDadosPessoais && string.IsNullOrEmpty(dto.ConfirmarSenha))
            throw new RegraDeNegocioException("Confirme a senha.");
        if (dto.ConfirmarSenha is not null && dto.ConfirmarSenha != dto.Senha)
            throw new RegraDeNegocioException("As senhas não conferem.");

        // CPF
        if (cpf.Length == 0)
        {
            if (exigirDadosPessoais) throw new RegraDeNegocioException("Informe o CPF.");
        }
        else if (!Validacoes.CpfValido(cpf))
        {
            throw new RegraDeNegocioException("CPF inválido.");
        }

        // Telefone
        if (telefone.Length == 0)
        {
            if (exigirDadosPessoais) throw new RegraDeNegocioException("Informe um telefone com DDD.");
        }
        else if (telefone.Length is < 10 or > 11)
        {
            throw new RegraDeNegocioException("Informe um telefone com DDD (10 ou 11 dígitos).");
        }

        // Data de nascimento (18 anos ou mais)
        if (dto.Nascimento is DateOnly nascimento)
            ValidarNascimento(nascimento);
        else if (exigirDadosPessoais)
            throw new RegraDeNegocioException("Informe sua data de nascimento.");

        if (_repositorio.EmailEmUso(email))
            throw new ConflitoException("Este e-mail já está cadastrado.");

        if (cpf.Length > 0 && _repositorio.CpfEmUso(cpf))
            throw new ConflitoException("Este CPF já está cadastrado.");

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Cpf = cpf.Length > 0 ? cpf : null,
            Telefone = telefone.Length > 0 ? telefone : null,
            Nascimento = dto.Nascimento,
            SenhaHash = SenhaHasher.Gerar(dto.Senha),
            Perfil = Perfis.Cliente,
            Ativo = true,
            TrocarSenha = false
        };

        try
        {
            _repositorio.Adicionar(usuario);
        }
        catch (ConflitoDeGravacaoException)
        {
            // Duas pessoas cadastrando o mesmo e-mail no mesmo instante: o índice único barra.
            throw new ConflitoException("Este e-mail ou CPF já está cadastrado.");
        }

        return usuario;
    }

    // ----------------------------------------------------------- dados da conta

    public Usuario AtualizarPerfil(int usuarioId, AtualizarPerfilDto dto)
    {
        var usuario = ObterPorId(usuarioId);

        var nome = Validacoes.NormalizarTexto(dto.Nome);
        var telefone = Validacoes.SomenteDigitos(dto.Telefone);

        ValidarNome(nome);
        if (telefone.Length > 0 && telefone.Length is < 10 or > 11)
            throw new RegraDeNegocioException("Informe um telefone com DDD (10 ou 11 dígitos).");

        usuario.Nome = nome;
        usuario.Telefone = telefone.Length > 0 ? telefone : null;
        _repositorio.Atualizar(usuario);
        return usuario;
    }

    // ---------------------------------------------------------- troca de senha

    public Usuario AlterarSenha(int usuarioId, AlterarSenhaDto dto)
    {
        var usuario = ObterPorId(usuarioId);

        // 400 (e não 401): o 401 faria o app achar que a sessão expirou.
        if (!SenhaHasher.Verificar(dto.SenhaAtual, usuario.SenhaHash))
            throw new RegraDeNegocioException("A senha atual está incorreta.");

        ValidarSenha(dto.NovaSenha);

        if (dto.ConfirmarNovaSenha is not null && dto.ConfirmarNovaSenha != dto.NovaSenha)
            throw new RegraDeNegocioException("A confirmação não confere com a nova senha.");

        if (dto.NovaSenha == dto.SenhaAtual)
            throw new RegraDeNegocioException("A nova senha deve ser diferente da atual.");

        usuario.SenhaHash = SenhaHasher.Gerar(dto.NovaSenha);
        usuario.TrocarSenha = false;
        _repositorio.Atualizar(usuario);
        return usuario;
    }

    // ---------------------------------------------------- equipe (só gerente)

    public List<Usuario> ListarEquipe() => _repositorio.ListarEquipe();

    // Funcionário novo nasce com senha temporária e TrocarSenha = true:
    // no primeiro acesso ele é obrigado a definir uma senha só dele.
    // Se o gerente não digitar a senha temporária, o sistema gera uma.
    public FuncionarioCriadoDto CriarFuncionario(FuncionarioCriarDto dto)
    {
        var nome = Validacoes.NormalizarTexto(dto.Nome);
        var email = Validacoes.NormalizarEmail(dto.Email);
        var perfil = PerfilDaEquipe(dto.Perfil)
            ?? throw new RegraDeNegocioException("Perfil inválido. Use \"Funcionario\" ou \"Gerente\".");

        ValidarNomeCompleto(nome, "Informe o nome completo do funcionário.");

        if (!Validacoes.EmailValido(email))
            throw new RegraDeNegocioException("Informe um e-mail válido.");

        var senha = string.IsNullOrWhiteSpace(dto.SenhaTemporaria)
            ? SenhaHasher.GerarSenhaTemporaria()
            : dto.SenhaTemporaria.Trim();
        ValidarSenha(senha);

        if (_repositorio.EmailEmUso(email))
            throw new ConflitoException("Este e-mail já está cadastrado.");

        var usuario = new Usuario
        {
            Nome = nome,
            Email = email,
            Perfil = perfil,
            SenhaHash = SenhaHasher.Gerar(senha),
            Ativo = true,
            TrocarSenha = true
        };

        try
        {
            _repositorio.Adicionar(usuario);
        }
        catch (ConflitoDeGravacaoException)
        {
            throw new ConflitoException("Este e-mail já está cadastrado.");
        }

        return new FuncionarioCriadoDto { Funcionario = FuncionarioDto.De(usuario), SenhaTemporaria = senha };
    }

    // Desativa ou reativa o acesso de alguém da equipe.
    public Usuario AlterarAtivo(int gerenteId, int funcionarioId, bool ativo)
    {
        if (gerenteId == funcionarioId)
            throw new RegraDeNegocioException("Você não pode desativar a própria conta.");

        var usuario = ObterDaEquipe(funcionarioId);
        usuario.Ativo = ativo;
        _repositorio.Atualizar(usuario);
        return usuario;
    }

    // Funcionário esqueceu a senha: o sistema gera uma nova senha temporária,
    // que o gerente vê uma vez e passa para ele.
    public FuncionarioCriadoDto RedefinirSenha(int gerenteId, int funcionarioId)
    {
        if (gerenteId == funcionarioId)
            throw new RegraDeNegocioException("Para mudar a sua própria senha, use \"Alterar senha\".");

        var usuario = ObterDaEquipe(funcionarioId);
        var senha = SenhaHasher.GerarSenhaTemporaria();

        usuario.SenhaHash = SenhaHasher.Gerar(senha);
        usuario.TrocarSenha = true;
        _repositorio.Atualizar(usuario);

        return new FuncionarioCriadoDto { Funcionario = FuncionarioDto.De(usuario), SenhaTemporaria = senha };
    }

    // ------------------------------------------------- gerente do primeiro uso

    // Sem nenhum gerente no banco ninguém consegue entrar no painel para criar a
    // equipe. Por isso, ao subir a Web ou a Api, se não existir gerente ativo, cria
    // um com os dados do appsettings.json (seção "GerenteInicial"), com TrocarSenha = true.
    // Devolve true quando criou.
    public bool GarantirGerenteInicial(string nome, string email, string senha)
    {
        if (_repositorio.ExisteGerenteAtivo()) return false;

        email = Validacoes.NormalizarEmail(email);
        if (!Validacoes.EmailValido(email) || _repositorio.EmailEmUso(email)) return false;

        var problema = Validacoes.ProblemaNaSenha(senha);
        if (problema is not null)
            throw new InvalidOperationException($"GerenteInicial:Senha do appsettings.json não serve: {problema}");

        try
        {
            _repositorio.Adicionar(new Usuario
            {
                Nome = Validacoes.NormalizarTexto(nome),
                Email = email,
                Perfil = Perfis.Gerente,
                SenhaHash = SenhaHasher.Gerar(senha),
                Ativo = true,
                TrocarSenha = true
            });
            return true;
        }
        catch (ConflitoDeGravacaoException)
        {
            // A Web e a Api subiram juntas e a outra criou primeiro.
            return false;
        }
    }

    // ------------------------------------------------------------- auxiliares

    private Usuario ObterDaEquipe(int id)
    {
        var usuario = _repositorio.ObterPorId(id);
        if (usuario is null || !Perfis.EhEquipe(usuario.Perfil))
            throw new NaoEncontradoException("Funcionário não encontrado.");
        return usuario;
    }

    // "gerente", "GERENTE", "Gerente" -> "Gerente"; "funcionário" -> "Funcionario".
    private static string? PerfilDaEquipe(string? perfil)
    {
        var texto = Validacoes.RemoverAcentos((perfil ?? string.Empty).Trim());
        if (texto.Equals(Perfis.Funcionario, StringComparison.OrdinalIgnoreCase)) return Perfis.Funcionario;
        if (texto.Equals(Perfis.Gerente, StringComparison.OrdinalIgnoreCase)) return Perfis.Gerente;
        return null;
    }

    private static void ValidarNome(string nome)
    {
        if (nome.Length < 2)
            throw new RegraDeNegocioException("Informe seu nome.");
        if (nome.Length > 120)
            throw new RegraDeNegocioException("O nome pode ter no máximo 120 caracteres.");
        if (Validacoes.TemCaracterePerigoso(nome))
            throw new RegraDeNegocioException("O nome não pode ter os caracteres < ou >.");
    }

    private static void ValidarNomeCompleto(string nome, string mensagem)
    {
        ValidarNome(nome);
        if (!Validacoes.NomeCompleto(nome))
            throw new RegraDeNegocioException(mensagem);
    }

    private static void ValidarSenha(string? senha)
    {
        var problema = Validacoes.ProblemaNaSenha(senha);
        if (problema is not null)
            throw new RegraDeNegocioException(problema);
    }

    private static void ValidarNascimento(DateOnly nascimento)
    {
        var hoje = DateOnly.FromDateTime(DateTime.Today);
        if (nascimento > hoje || nascimento.Year < 1900)
            throw new RegraDeNegocioException("Data de nascimento inválida.");

        if (Validacoes.Idade(nascimento, hoje) < 18)
            throw new RegraDeNegocioException("Você precisa ter pelo menos 18 anos para se cadastrar.");
    }
}
