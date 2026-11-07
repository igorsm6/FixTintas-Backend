/* ==========================================================================
   site.js — comportamento das telas da loja (o mesmo do script.js original).

   O que mudou em relação ao site antigo: o JavaScript NÃO guarda mais produtos,
   preços nem carrinho. Tudo vem do servidor (Razor + Negocio):
     - os cards da vitrine são desenhados pelo Razor (_CardProduto.cshtml);
     - "Adicionar ao carrinho" envia um formulário para /Carrinho/Adicionar;
     - o carrinho fica na sessão, no servidor.
   Aqui ficam só as partes visuais: carrossel, menus, janelas e sugestões da busca.
   ========================================================================== */
(function () {
    'use strict';

    var moeda = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

    function $(seletor, raiz) { return (raiz || document).querySelector(seletor); }
    function $$(seletor, raiz) { return Array.prototype.slice.call((raiz || document).querySelectorAll(seletor)); }

    // Tira acentos e deixa minúsculo ("Látex" -> "latex"), para a busca achar com ou sem acento.
    function normalizar(texto) {
        return String(texto || '').normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
    }

    /* ---------------------------------------------------------- carrossel */
    function iniciarCarrossel() {
        var slides = $$('.carrossel .slide');
        if (slides.length === 0) return;
        var atual = 0;

        function mostrar(indice) {
            slides.forEach(function (s) { s.classList.remove('ativo'); });
            slides[indice].classList.add('ativo');
        }
        function proximo() { atual = (atual + 1) % slides.length; mostrar(atual); }
        function anterior() { atual = (atual - 1 + slides.length) % slides.length; mostrar(atual); }

        var btnProximo = $('.btn-carrossel.proximo');
        var btnAnterior = $('.btn-carrossel.anterior');
        if (btnProximo) btnProximo.addEventListener('click', proximo);
        if (btnAnterior) btnAnterior.addEventListener('click', anterior);
        setInterval(proximo, 5000);
    }

    /* --------------------------------------------------- menu de cima (celular) */
    function iniciarMenuCelular() {
        var botao = $('#nav-toggle');
        var menu = $('#nav-menu');
        var acoes = $('.nav-mobile-acoes');
        if (!botao || !menu) return;

        botao.addEventListener('click', function () {
            var aberto = menu.classList.toggle('aberto');
            if (acoes) acoes.classList.toggle('aberto', aberto);
            botao.setAttribute('aria-expanded', aberto ? 'true' : 'false');
        });
        $$('a', menu).forEach(function (link) {
            link.addEventListener('click', function () {
                menu.classList.remove('aberto');
                if (acoes) acoes.classList.remove('aberto');
            });
        });
    }

    /* ------------------------------------------------------- menu lateral */
    function iniciarMenuLateral() {
        var botao = $('#sidebar-toggle');
        var conteudo = $('#sidebar-conteudo');
        if (botao && conteudo) {
            botao.addEventListener('click', function () {
                var aberto = conteudo.classList.toggle('aberto');
                botao.classList.toggle('aberto', aberto);
                botao.setAttribute('aria-expanded', aberto ? 'true' : 'false');
            });
        }

        // A barra lateral começa logo abaixo do menu de cima (que muda de altura).
        var nav = $('nav');
        var lateral = $('.sidebar-filtro');
        function ajustarAltura() {
            if (!nav || !lateral) return;
            if (window.matchMedia('(max-width: 1024px)').matches) {
                lateral.style.top = '';
                lateral.style.height = '';
                return;
            }
            lateral.style.top = nav.offsetHeight + 'px';
            lateral.style.height = 'calc(100vh - ' + nav.offsetHeight + 'px)';
        }
        ajustarAltura();
        window.addEventListener('resize', ajustarAltura);

        // Coluna 2: as subcategorias da categoria clicada (os painéis já vêm prontos do Razor).
        function abrirColuna(id) {
            $$('.painel-coluna').forEach(function (p) { p.hidden = p.id !== id; });
            $$('.categoria[data-coluna]').forEach(function (c) { c.classList.toggle('ativo', c.dataset.coluna === id); });
            document.body.classList.add('col-2-open');

            // No celular a coluna 2 fica embaixo da 1: rola até ela.
            var painel = document.getElementById(id);
            if (painel && window.matchMedia('(max-width: 768px)').matches) {
                painel.scrollIntoView({ behavior: 'smooth', block: 'start' });
            }
        }
        function fecharColuna() {
            $$('.painel-coluna').forEach(function (p) { p.hidden = true; });
            $$('.categoria[data-coluna]').forEach(function (c) { c.classList.remove('ativo'); });
            document.body.classList.remove('col-2-open');
        }

        $$('.categoria[data-coluna]').forEach(function (item) {
            item.addEventListener('click', function () { abrirColuna(item.dataset.coluna); });
            item.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); abrirColuna(item.dataset.coluna); }
            });
        });
        $$('[data-fechar-coluna]').forEach(function (b) { b.addEventListener('click', fecharColuna); });
    }

    /* ---------------------------------------------------- painel do carrinho */
    function iniciarCarrinho() {
        var painel = $('#painel-carrinho');
        if (!painel) return;

        function abrir() { painel.hidden = false; }
        function fechar() { painel.hidden = true; }

        $$('[data-abrir-carrinho]').forEach(function (b) { b.addEventListener('click', abrir); });
        $$('[data-fechar-carrinho]').forEach(function (b) { b.addEventListener('click', fechar); });
        painel.addEventListener('click', function (e) { if (e.target === painel) fechar(); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') fechar(); });
    }

    /* ------------------------------------------------------- menu do usuário */
    function iniciarMenuUsuario() {
        var menu = $('#fx-menu-usuario');
        if (!menu) return;

        function fechar() {
            menu.hidden = true;
            $$('[data-menu-usuario]').forEach(function (b) { b.setAttribute('aria-expanded', 'false'); });
        }

        $$('[data-menu-usuario]').forEach(function (botao) {
            botao.addEventListener('click', function (e) {
                e.stopPropagation();
                if (!menu.hidden) { fechar(); return; }
                var r = botao.getBoundingClientRect();
                menu.style.top = (r.bottom + 6) + 'px';
                menu.style.right = Math.max(8, window.innerWidth - r.right) + 'px';
                menu.hidden = false;
                botao.setAttribute('aria-expanded', 'true');
            });
        });
        document.addEventListener('click', function (e) { if (!menu.contains(e.target)) fechar(); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') fechar(); });
        window.addEventListener('resize', fechar);
    }

    /* ------------------------------------- janela "Adicionar ao carrinho" */
    function iniciarJanelaProduto() {
        var janela = $('#modal-produto');
        var form = $('#form-produto');
        if (!janela || !form) return;

        var campoProduto = $('#campo-produto');
        var campoCor = $('#campo-cor');
        var campoQuantidade = $('#campo-quantidade');
        var produto = null;      // dados do card clicado (data-produto)
        var variacao = null;     // opção escolhida (volume/tamanho)
        var quantidade = 1;

        function erro(texto) { $('#modal-erro').textContent = texto || ''; }

        function maximo() { return variacao ? Math.min(variacao.estoque, 1000) : 1; }

        function atualizarQuantidade() {
            quantidade = Math.max(1, Math.min(quantidade, maximo()));
            $('#modal-qtd').textContent = quantidade;
            campoQuantidade.value = quantidade;
        }

        function escolherVariacao(v) {
            variacao = v;
            campoProduto.value = v ? v.id : '';
            $$('#modal-volumes .opcao-btn').forEach(function (b) {
                var ativo = v && Number(b.dataset.id) === v.id;
                b.classList.toggle('ativo', !!ativo);
                b.setAttribute('aria-checked', ativo ? 'true' : 'false');
            });

            var preco = $('#modal-preco');
            var precoDe = $('#modal-preco-de');
            var estoque = $('#modal-estoque');
            if (v) {
                preco.textContent = moeda.format(v.preco);
                if (v.precoDe) {
                    precoDe.innerHTML = 'De <s></s> por';
                    $('s', precoDe).textContent = moeda.format(v.precoDe);
                    precoDe.hidden = false;
                } else {
                    precoDe.hidden = true;
                }
                estoque.textContent = v.estoque <= 5 ? 'Só ' + v.estoque + ' em estoque' : v.estoque + ' em estoque';
            } else {
                var mais = produto.variacoes.reduce(function (a, b) { return a.preco <= b.preco ? a : b; });
                preco.textContent = 'a partir de ' + moeda.format(mais.preco);
                precoDe.hidden = true;
                estoque.textContent = '';
            }
            atualizarQuantidade();
        }

        function escolherCor(botao) {
            $$('.cor-item', janela).forEach(function (c) {
                c.classList.toggle('ativa', c === botao);
                c.setAttribute('aria-checked', c === botao ? 'true' : 'false');
            });
            campoCor.value = botao ? botao.dataset.corId : '';
            $('#cor-selecionada').textContent = botao ? 'Cor: ' + botao.dataset.cor : 'Nenhuma cor selecionada';
            if (botao) erro('');
        }

        function abrir(dados) {
            produto = dados;
            quantidade = 1;
            erro('');

            $('#modal-nome').textContent = dados.titulo;
            $('#modal-marca').textContent = [dados.marca, dados.categoria].filter(Boolean).join(' · ');
            $('#modal-descricao').textContent = dados.descricao || '';
            $('#secao-descricao').hidden = !dados.descricao;

            var imagem = $('#modal-img');
            imagem.textContent = '';
            if (dados.imagem) {
                var img = document.createElement('img');
                img.src = dados.imagem;
                img.alt = dados.titulo;
                img.addEventListener('error', function () { imagem.textContent = dados.categoria || dados.titulo; });
                imagem.appendChild(img);
            } else {
                imagem.textContent = dados.categoria || dados.titulo;
            }

            // Cor e acabamento: só para tinta.
            $('#secao-cor').hidden = !dados.ehTinta;
            $('#secao-acabamento').hidden = !dados.ehTinta || !dados.acabamento;
            $('#modal-acabamento').textContent = dados.acabamento || '';
            escolherCor(null);

            // Volume / tamanho: um botão por opção do card.
            var opcoes = dados.variacoes;
            var rotulos = opcoes.map(function (v) { return v.rotulo; });
            var soUnidade = opcoes.length === 1 && rotulos[0] === 'Unidade';
            $('#label-volume').textContent = dados.ehTinta ? 'Volume'
                : rotulos.some(function (r) { return /degrau/i.test(r); }) ? 'Degraus' : 'Tamanho';
            $('#secao-volume').hidden = soUnidade;

            var lista = $('#modal-volumes');
            lista.textContent = '';
            opcoes.forEach(function (v) {
                var b = document.createElement('button');
                b.type = 'button';
                b.className = 'opcao-btn';
                b.dataset.id = v.id;
                b.setAttribute('role', 'radio');
                b.setAttribute('aria-checked', 'false');
                b.textContent = v.rotulo;
                if (v.estoque <= 0) {
                    b.disabled = true;
                    b.title = 'Esgotado';
                } else if (opcoes.length > 1) {
                    var p = document.createElement('small');
                    p.textContent = moeda.format(v.preco);
                    b.appendChild(p);
                }
                b.addEventListener('click', function () { escolherVariacao(v); erro(''); });
                lista.appendChild(b);
            });

            // Com uma opção só (ou uma só em estoque), ela já vem escolhida.
            var emEstoque = opcoes.filter(function (v) { return v.estoque > 0; });
            escolherVariacao(emEstoque.length === 1 ? emEstoque[0] : null);

            janela.hidden = false;
            var fechar = $('[data-fechar-produto]', janela);
            if (fechar) fechar.focus();
        }

        function fecharJanela() { janela.hidden = true; }

        // Cada card guarda os seus dados em data-produto (JSON montado pelo Razor).
        document.addEventListener('click', function (e) {
            var botao = e.target.closest('[data-abrir-produto]');
            if (!botao) return;
            var card = botao.closest('.produto');
            if (!card || !card.dataset.produto) return;
            try {
                abrir(JSON.parse(card.dataset.produto));
            } catch (ex) {
                console.error('Dados do produto inválidos', ex);
            }
        });

        $$('.cor-item', janela).forEach(function (c) {
            c.addEventListener('click', function () { escolherCor(c); });
        });
        $$('[data-quantidade]', janela).forEach(function (b) {
            b.addEventListener('click', function () {
                quantidade += Number(b.dataset.quantidade);
                atualizarQuantidade();
            });
        });
        $$('[data-fechar-produto]', janela).forEach(function (b) { b.addEventListener('click', fecharJanela); });
        janela.addEventListener('click', function (e) { if (e.target === janela) fecharJanela(); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') fecharJanela(); });

        // Confere antes de enviar (o servidor confere de novo).
        form.addEventListener('submit', function (e) {
            if (!produto) { e.preventDefault(); return; }
            if (produto.ehTinta && !campoCor.value) {
                e.preventDefault();
                erro('Escolha uma cor.');
                return;
            }
            if (!variacao) {
                e.preventDefault();
                erro(produto.ehTinta ? 'Escolha o volume.' : 'Escolha o tamanho.');
                return;
            }
            if (!produto.ehTinta) campoCor.value = '';
        });
    }

    /* ------------------------------------------------ sugestões da busca */
    function iniciarBusca() {
        var campo = $('#input-busca');
        var lista = $('#dropdown-busca');
        var dados = $('#dados-busca');
        if (!campo || !lista || !dados) return;

        // O endereço da página de produtos vem do próprio formulário da busca (action="/Produtos").
        var destino = (campo.form && campo.form.getAttribute('action')) || '/Produtos';
        var produtos = [];
        try { produtos = JSON.parse(dados.textContent || '[]'); } catch (ex) { produtos = []; }
        produtos.forEach(function (p) { p.busca = normalizar(p.nome + ' ' + p.sub); });

        function fechar() { lista.classList.remove('ativo'); lista.textContent = ''; }

        campo.addEventListener('input', function () {
            var termo = normalizar(campo.value);
            lista.textContent = '';
            if (!termo) { fechar(); return; }

            var achados = produtos.filter(function (p) { return p.busca.indexOf(termo) >= 0; }).slice(0, 6);
            if (achados.length === 0) {
                var vazio = document.createElement('div');
                vazio.className = 'dropdown-vazio';
                vazio.textContent = 'Nenhum produto encontrado';
                lista.appendChild(vazio);
            }
            achados.forEach(function (p) {
                var item = document.createElement('a');
                item.className = 'dropdown-item';
                item.href = destino + '?termo=' + encodeURIComponent(p.nome);
                var nome = document.createElement('div');
                nome.className = 'dropdown-item-nome';
                nome.textContent = p.nome;
                var sub = document.createElement('div');
                sub.className = 'dropdown-item-sub';
                sub.textContent = p.sub;
                item.appendChild(nome);
                item.appendChild(sub);
                lista.appendChild(item);
            });
            lista.classList.add('ativo');
        });

        document.addEventListener('click', function (e) {
            if (!campo.contains(e.target) && !lista.contains(e.target)) fechar();
        });
        campo.addEventListener('keydown', function (e) { if (e.key === 'Escape') fechar(); });
    }

    /* -------------------------------------------- avisos e pequenos detalhes */
    function iniciarAvisos() {
        $$('[data-some-sozinho]').forEach(function (aviso) {
            function sumir() {
                aviso.classList.add('sumindo');
                setTimeout(function () { aviso.remove(); }, 450);
            }
            var fechar = $('.fx-aviso-fechar', aviso);
            if (fechar) fechar.addEventListener('click', sumir);
            setTimeout(sumir, aviso.classList.contains('erro') ? 7000 : 4500);
        });
    }

    // Imagem que não carregou: mostra o texto de reserva (ou só esconde a imagem quebrada).
    function iniciarImagens() {
        function trocarPorTexto(img) {
            var texto = document.createElement('span');
            texto.textContent = img.dataset.semImagem;
            img.replaceWith(texto);
        }
        function esconder(img) { img.style.visibility = 'hidden'; }

        $$('img[data-sem-imagem]').forEach(function (img) {
            if (img.complete && img.naturalWidth === 0) trocarPorTexto(img);
            else img.addEventListener('error', function () { trocarPorTexto(img); });
        });
        $$('img[data-esconder-se-faltar]').forEach(function (img) {
            if (img.complete && img.naturalWidth === 0) esconder(img);
            else img.addEventListener('error', function () { esconder(img); });
        });
    }

    // Formulários com data-confirmar pedem confirmação antes de enviar (cancelar pedido...).
    function iniciarConfirmacoes() {
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (form.dataset && form.dataset.confirmar && !window.confirm(form.dataset.confirmar)) {
                e.preventDefault();
            }
        });
        $$('select[data-enviar-ao-mudar]').forEach(function (s) {
            s.addEventListener('change', function () { if (s.form) s.form.submit(); });
        });
    }

    // Máscaras simples de CPF e telefone (o servidor aceita com ou sem pontuação).
    function iniciarMascaras() {
        function cpf(v) {
            v = v.replace(/\D/g, '').slice(0, 11);
            return v.replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d)/, '$1.$2').replace(/(\d{3})(\d{1,2})$/, '$1-$2');
        }
        function telefone(v) {
            v = v.replace(/\D/g, '').slice(0, 11);
            if (v.length <= 2) return v.length ? '(' + v : '';
            if (v.length <= 6) return '(' + v.slice(0, 2) + ') ' + v.slice(2);
            if (v.length <= 10) return '(' + v.slice(0, 2) + ') ' + v.slice(2, 6) + '-' + v.slice(6);
            return '(' + v.slice(0, 2) + ') ' + v.slice(2, 7) + '-' + v.slice(7);
        }
        $$('input[data-mascara]').forEach(function (campo) {
            var formatar = campo.dataset.mascara === 'cpf' ? cpf : telefone;
            campo.addEventListener('input', function () { campo.value = formatar(campo.value); });
            if (campo.value) campo.value = formatar(campo.value);
        });
    }

    /* ------------------------------------------ caixa "A senha precisa ter:" */
    // Marca cada regra (verde, com ✓) conforme a pessoa digita a senha.
    // São as mesmas regras de FixTintas.Modelos/RegrasDaSenha.cs, que o servidor
    // confere de verdade. Mudou lá? Mude aqui também.
    function iniciarRegrasSenha() {
        $$('[data-regras-senha]').forEach(function (caixa) {
            var campo = document.getElementById(caixa.dataset.regrasSenha);
            if (!campo) return;
            var testes = {
                tamanho: function (s) { return s.length >= 8; },
                letra: function (s) { return /[A-Za-zÀ-ÖØ-öø-ÿ]/.test(s); },
                numero: function (s) { return /[0-9]/.test(s); }
            };
            function atualizar() {
                $$('[data-regra]', caixa).forEach(function (item) {
                    item.classList.toggle('ok', testes[item.dataset.regra](campo.value));
                });
            }
            campo.addEventListener('input', atualizar);
            atualizar();
        });
    }

    /* ------------------------------------------------- app para Android */
    // O app FixTintas (o app Flutter) é só para Android. Esta parte:
    //   1) mostra os avisos que dependem do aparelho (data-so-computador e data-so-ios);
    //   2) abre a janela "Baixe o app" para quem está num celular Android
    //      (Views/Shared/_JanelaApp.cshtml). Quem fecha a janela não a vê de novo por
    //      7 dias; quem baixa o app, por 30 dias (cookie fx_aviso_app).
    var COOKIE_APP = 'fx_aviso_app';

    function ehAndroid() {
        var dados = navigator.userAgentData;
        if (dados && typeof dados.platform === 'string' && /android/i.test(dados.platform)) return true;
        return /android/i.test(navigator.userAgent);
    }

    function ehIos() {
        // O iPad se apresenta como Mac, mas tem tela de toque.
        return /iphone|ipad|ipod/i.test(navigator.userAgent) ||
            (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    }

    function ehCelularOuTablet() {
        return /mobi|android|iphone|ipad|ipod|tablet/i.test(navigator.userAgent) || ehIos();
    }

    function temCookie(nome) {
        try {
            return document.cookie.split(';').some(function (c) { return c.trim().indexOf(nome + '=') === 0; });
        } catch (ex) {
            return false;
        }
    }

    function gravarCookie(nome, dias) {
        try {
            document.cookie = nome + '=1; max-age=' + (dias * 86400) + '; path=/; SameSite=Lax';
        } catch (ex) { /* sem cookie, a janela pode aparecer de novo; não é grave */ }
    }

    function iniciarApp() {
        var android = ehAndroid();
        var ios = !android && ehIos();
        var computador = !android && !ios && !ehCelularOuTablet();
        $$('[data-so-ios]').forEach(function (el) { el.hidden = !ios; });
        $$('[data-so-computador]').forEach(function (el) { el.hidden = !computador; });
        // No iPhone o .apk não instala: o botão de baixar some (fica o aviso "só para Android").
        if (ios) $$('[data-baixar-app]').forEach(function (el) { el.hidden = true; });

        // Quem já clicou em "Baixar" (em qualquer lugar do site) não precisa do convite tão cedo.
        $$('[data-baixar-app]').forEach(function (link) {
            link.addEventListener('click', function () { gravarCookie(COOKIE_APP, 30); });
        });

        var janela = $('#janela-app');
        if (!janela) return;
        var convite = $('[data-etapa="convite"]', janela);
        var baixando = $('[data-etapa="baixando"]', janela);

        function fechar() {
            if (janela.hidden) return;
            janela.hidden = true;
            if (!temCookie(COOKIE_APP)) gravarCookie(COOKIE_APP, 7);
        }

        $$('[data-fechar-app]', janela).forEach(function (b) { b.addEventListener('click', fechar); });
        janela.addEventListener('click', function (e) { if (e.target === janela) fechar(); });
        document.addEventListener('keydown', function (e) { if (e.key === 'Escape') fechar(); });

        // O link baixa o arquivo (a página continua aberta); a janela passa a mostrar
        // os passos para instalar.
        var caixa = $('.fx-janela-app', janela);
        $$('[data-baixar-app]', janela).forEach(function (link) {
            link.addEventListener('click', function () {
                setTimeout(function () {
                    convite.hidden = true;
                    baixando.hidden = false;
                    if (caixa) caixa.focus();
                }, 400);
            });
        });

        if (!android || temCookie(COOKIE_APP)) return;

        setTimeout(function () {
            // Não abre por cima de outra janela (produto ou carrinho).
            if ($('.modal-overlay:not([hidden]), .carrinho-overlay:not([hidden])')) return;
            janela.hidden = false;
            // O foco vai para a janela (leitor de tela anuncia o título), sem destacar um botão.
            if (caixa) caixa.focus();
        }, 1500);
    }

    document.addEventListener('DOMContentLoaded', function () {
        iniciarCarrossel();
        iniciarMenuCelular();
        iniciarMenuLateral();
        iniciarCarrinho();
        iniciarMenuUsuario();
        iniciarJanelaProduto();
        iniciarBusca();
        iniciarAvisos();
        iniciarImagens();
        iniciarConfirmacoes();
        iniciarMascaras();
        iniciarRegrasSenha();
        iniciarApp();
    });
})();
