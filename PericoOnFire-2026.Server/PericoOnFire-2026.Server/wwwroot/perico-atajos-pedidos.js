// Atajos del módulo Pedidos. No modifica información ni envía comandas.
(function () {
    function estaEnPedidos() {
        return window.location.pathname.replace(/\/$/, "").toLowerCase() === "/pedidos";
    }

    function enfocarBusquedaAlAbrir() {
        const objetivo = "#perico-buscar-cliente-pedido";
        const enfocar = () => {
            const input = document.querySelector(objetivo);
            if (!input) return false;
            input.focus();
            return true;
        };
        if (enfocar()) return;
        const observador = new MutationObserver(() => {
            if (enfocar()) observador.disconnect();
        });
        observador.observe(document.body, { childList: true, subtree: true });
        // No dejar observadores vivos si se cambia de página o falla la carga.
        setTimeout(() => observador.disconnect(), 2500);
    }

    document.addEventListener("keydown", function (evento) {
        if (!estaEnPedidos() || evento.repeat || evento.altKey || evento.metaKey) return;

        if (evento.ctrlKey && (evento.code === "Space" || evento.key === " ")) {
            const abrir = document.getElementById("perico-nuevo-pedido-abrir");
            if (!abrir) return; // Sin botón no hay permisos para cargar.
            evento.preventDefault();
            evento.stopPropagation();
            const modal = document.getElementById("perico-nuevo-pedido-cerrar");
            if (!modal) {
                abrir.click();
                enfocarBusquedaAlAbrir();
            } else {
                document.getElementById("perico-buscar-cliente-pedido")?.focus();
            }
        }

        if (!evento.ctrlKey && evento.key === "Escape") {
            const cerrar = document.getElementById("perico-nuevo-pedido-cerrar");
            if (!cerrar || cerrar.disabled) return;
            evento.preventDefault();
            cerrar.click();
            document.getElementById("perico-nuevo-pedido-abrir")?.focus();
        }
    }, true);
})();
