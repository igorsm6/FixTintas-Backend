/* Mensagens padrão do jQuery Validation em português.
   As mensagens dos campos vêm dos DTOs ([Required(ErrorMessage = "...")]);
   estas aparecem só nas regras tiradas do HTML (min, max, required...). */
(function ($) {
    'use strict';
    if (!$ || !$.validator) return;

    $.extend($.validator.messages, {
        required: 'Preencha este campo.',
        remote: 'Corrija este campo.',
        email: 'Informe um e-mail válido.',
        url: 'Informe um endereço válido.',
        date: 'Informe uma data válida.',
        dateISO: 'Informe uma data válida.',
        number: 'Informe um número válido.',
        digits: 'Informe somente números.',
        equalTo: 'Os valores não conferem.',
        maxlength: $.validator.format('Informe no máximo {0} caracteres.'),
        minlength: $.validator.format('Informe pelo menos {0} caracteres.'),
        rangelength: $.validator.format('Informe de {0} a {1} caracteres.'),
        range: $.validator.format('Informe um valor entre {0} e {1}.'),
        max: $.validator.format('Informe um valor menor ou igual a {0}.'),
        min: $.validator.format('Informe um valor maior ou igual a {0}.'),
        step: $.validator.format('Informe um múltiplo de {0}.')
    });
})(window.jQuery);
