// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Formato automático cédula Costa Rica X-XXXX-XXXX
(function () {
    function formatCedula(value) {
        var digits = (value || '').replace(/\D/g, '');
        if (digits.length === 0) return '';
        if (digits.length <= 1) return digits;
        if (digits.length <= 5) return digits.slice(0, 1) + '-' + digits.slice(1);
        return digits.slice(0, 1) + '-' + digits.slice(1, 5) + '-' + digits.slice(5, 9);
    }
    function onCedulaInput(e) {
        var el = e.target;
        var cursor = el.selectionStart;
        var oldVal = el.value;
        var oldLen = oldVal.length;
        var newVal = formatCedula(oldVal);
        el.value = newVal;
        var newLen = newVal.length;
        var newCursor = Math.max(0, cursor + (newLen - oldLen));
        if (newCursor > newVal.length) newCursor = newVal.length;
        el.setSelectionRange(newCursor, newCursor);
    }
    function initCedulaFormat() {
        var inputs = document.querySelectorAll('input#cedula, input#Cedula, input[name="cedula"], input[name="Cedula"], input[data-format-cedula], input.cedula-format');
        inputs.forEach(function (input) {
            if (input._cedulaFormatInit) return;
            input._cedulaFormatInit = true;
            input.addEventListener('input', onCedulaInput);
            input.addEventListener('paste', function (e) {
                setTimeout(function () { onCedulaInput({ target: input }); }, 0);
            });
            if (input.value) input.value = formatCedula(input.value);
        });
    }
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initCedulaFormat);
    } else {
        initCedulaFormat();
    }
    // Re-ejecutar cuando se carguen vistas parciales o dinámicas
    document.addEventListener('DOMContentLoaded', function () {
        setTimeout(initCedulaFormat, 100);
    });
})();
