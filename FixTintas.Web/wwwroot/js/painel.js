/* ==========================================================================
   painel.js — detalhes das telas do painel da equipe.
   As regras (preço só pelo gerente, estoque, exclusão...) ficam no servidor;
   aqui é só conforto: confirmações, pré-visualização da imagem etc.
   ========================================================================== */
(function () {
    'use strict';

    function $$(seletor, raiz) { return Array.prototype.slice.call((raiz || document).querySelectorAll(seletor)); }

    // Formulários com data-confirmar pedem confirmação antes de enviar (excluir, cancelar...).
    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (form.dataset && form.dataset.confirmar && !window.confirm(form.dataset.confirmar)) {
            e.preventDefault();
        }
    });

    // Filtros que se aplicam sozinhos ao mudar a opção.
    $$('select[data-enviar-ao-mudar]').forEach(function (s) {
        s.addEventListener('change', function () { if (s.form) s.form.submit(); });
    });

    // Produto: mostra "Volume" para tinta e "Tamanho" para acessório.
    // Os campos escondidos ficam desabilitados, então não são enviados nem validados.
    $$('select[data-tipo-produto]').forEach(function (tipo) {
        var form = tipo.form;
        function atualizar() {
            var ehTinta = tipo.value !== '';
            $$('[data-so-tinta]', form).forEach(function (el) { mostrar(el, ehTinta); });
            $$('[data-so-acessorio]', form).forEach(function (el) { mostrar(el, !ehTinta); });
        }
        function mostrar(el, visivel) {
            el.hidden = !visivel;
            $$('input, select, textarea', el).forEach(function (campo) { campo.disabled = !visivel; });
        }
        tipo.addEventListener('change', atualizar);
        atualizar();
    });

    // Pré-visualização da imagem escolhida (e aviso se passar de 2 MB).
    $$('input[type="file"][data-preview]').forEach(function (arquivo) {
        var caixa = document.getElementById(arquivo.dataset.preview);
        var original = caixa ? caixa.innerHTML : '';
        arquivo.addEventListener('change', function () {
            var f = arquivo.files && arquivo.files[0];
            if (!caixa) return;
            if (!f) { caixa.innerHTML = original; return; }
            if (f.size > 2 * 1024 * 1024) {
                window.alert('A imagem deve ter até 2 MB.');
                arquivo.value = '';
                caixa.innerHTML = original;
                return;
            }
            var img = document.createElement('img');
            img.alt = 'Pré-visualização';
            img.src = URL.createObjectURL(f);
            caixa.textContent = '';
            caixa.appendChild(img);
        });
    });

    // Cor: o seletor visual e o campo "#RRGGBB" andam juntos.
    $$('input[type="color"][data-sincroniza]').forEach(function (seletor) {
        var texto = document.getElementById(seletor.dataset.sincroniza);
        if (!texto) return;
        seletor.addEventListener('input', function () { texto.value = seletor.value.toUpperCase(); });
        texto.addEventListener('input', function () {
            if (/^#[0-9A-Fa-f]{6}$/.test(texto.value)) seletor.value = texto.value.toLowerCase();
        });
    });

    // "Gerar" senha temporária: 10 caracteres com letras e números (sem 0/O e 1/l, que confundem).
    $$('[data-gerar-senha]').forEach(function (botao) {
        botao.addEventListener('click', function () {
            var campo = document.getElementById(botao.dataset.gerarSenha);
            if (!campo) return;
            var letras = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz';
            var todos = letras + '23456789';
            var senha;
            do {
                var numeros = new Uint32Array(10);
                window.crypto.getRandomValues(numeros);
                senha = Array.prototype.map.call(numeros, function (n) { return todos[n % todos.length]; }).join('');
            } while (!/[A-Za-z]/.test(senha) || !/\d/.test(senha));
            campo.value = senha;
            campo.dispatchEvent(new Event('input', { bubbles: true }));
        });
    });

    // Imagem que não carregou vira o texto de reserva.
    $$('img[data-sem-imagem]').forEach(function (img) {
        function trocar() {
            var texto = document.createElement('span');
            texto.textContent = img.dataset.semImagem;
            img.replaceWith(texto);
        }
        if (img.complete && img.naturalWidth === 0) trocar();
        else img.addEventListener('error', trocar);
    });
})();
