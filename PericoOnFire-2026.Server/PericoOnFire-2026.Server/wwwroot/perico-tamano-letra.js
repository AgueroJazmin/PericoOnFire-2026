// Preferencia visual local al navegador. No se sincroniza con el servidor.
(function () {
    const clave = "perico-font-size-v1";
    function normalizar(valor) {
        const n = Number(valor);
        return Number.isFinite(n) ? Math.max(80, Math.min(130, Math.round(n / 5) * 5)) : 100;
    }
    function obtener() {
        try { return normalizar(localStorage.getItem(clave) ?? 100); }
        catch { return 100; }
    }
    function aplicar(valor) {
        const escala = normalizar(valor);
        document.documentElement.style.setProperty("--perico-escala-texto", String(escala / 100));
        document.documentElement.style.fontSize = "calc(16px * var(--perico-escala-texto))";
        return escala;
    }
    window.pericoTamanoLetra = {
        obtener,
        guardar(valor) {
            const normalizado = aplicar(valor);
            try { localStorage.setItem(clave, String(normalizado)); }
            catch { /* Sin almacenamiento disponible, queda aplicado en esta pestaña. */ }
            return normalizado;
        }
    };
    aplicar(obtener());
})();
