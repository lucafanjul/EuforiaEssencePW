(() => {
    "use strict";

    let carritoOcupado = false;

    function esFormularioCarrito(form) {
        const ruta = new URL(
            form.action,
            window.location.href
        ).pathname;

        return /\/Carrito\/(Agregar|Actualizar|CambiarCantidad|Quitar|Vaciar)\/?$/i
            .test(ruta);
    }

    async function confirmar(form) {
        const titulo =
            form.dataset.titulo || "¿Confirmás la operación?";

        const texto = form.dataset.mensaje || "";

        if (typeof Swal === "undefined") {
            return window.confirm(titulo + "\n" + texto);
        }

        const respuesta = await Swal.fire({
            title: titulo,
            text: texto,
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: form.dataset.boton || "Confirmar",
            cancelButtonText: "Volver",
            confirmButtonColor: "#b02a37",
            cancelButtonColor: "#6c757d",
            reverseButtons: true,
            focusCancel: true
        });

        return respuesta.isConfirmed;
    }

    function notificar(texto, tipo = "success") {
        if (typeof Swal !== "undefined") {
            Swal.fire({
                toast: true,
                position: "top-end",
                icon: tipo,
                title: texto,
                showConfirmButton: false,
                timer: tipo === "error" ? 6000 : 2800,
                timerProgressBar: true
            });

            return;
        }

        let aviso = document.getElementById("avisoCarrito");

        if (!aviso) {
            aviso = document.createElement("div");
            aviso.id = "avisoCarrito";
            aviso.setAttribute("role", "status");
            aviso.setAttribute("aria-live", "polite");
            document.body.appendChild(aviso);
        }

        aviso.className = "ee-aviso-carrito alert " +
            (tipo === "error" ? "alert-danger" : "alert-success");

        aviso.textContent = texto;
        aviso.hidden = false;
    }

    function actualizarContador(cantidad) {
        const contador = document.getElementById("contadorCarrito");
        const enlace = document.getElementById("enlaceCarrito");

        if (contador) {
            contador.textContent = String(cantidad);
            contador.hidden = cantidad === 0;
        }

        if (enlace) {
            enlace.setAttribute(
                "aria-label",
                "Carrito, " + cantidad + " unidades"
            );
        }
    }

    async function refrescarCarrito() {
        const contenedor = document.getElementById("contenidoCarrito");

        // Al agregar desde el catálogo solo actualizamos el contador.
        if (!contenedor) return;

        const respuesta = await fetch(contenedor.dataset.url, {
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            credentials: "same-origin",
            cache: "no-store"
        });

        const tipo = respuesta.headers.get("content-type") || "";

        if (!respuesta.ok ||
            respuesta.redirected ||
            !tipo.includes("text/html")) {
            throw new Error(
                "No se pudo actualizar la vista del carrito."
            );
        }

        contenedor.innerHTML = await respuesta.text();
    }

    document.addEventListener("submit", async event => {
        const form = event.target;

        if (!(form instanceof HTMLFormElement) ||
            event.defaultPrevented) {
            return;
        }

        const esCarrito = esFormularioCarrito(form);

        if (!esCarrito && !form.matches("[data-confirmar]")) {
            return;
        }

        // Envío normal de otros formularios, después de confirmar.
        if (!esCarrito && form.dataset.confirmado === "true") {
            delete form.dataset.confirmado;
            return;
        }

        event.preventDefault();

        // Conserva las confirmaciones del resto del sistema.
        if (!esCarrito) {
            if (form.dataset.confirmando === "true") return;

            form.dataset.confirmando = "true";

            const submitter = event.submitter;

            try {
                if (!await confirmar(form) || !form.isConnected) {
                    return;
                }

                form.dataset.confirmado = "true";

                if (submitter &&
                    submitter.form === form &&
                    !submitter.disabled) {
                    form.requestSubmit(submitter);
                } else {
                    form.requestSubmit();
                }
            } finally {
                delete form.dataset.confirmando;
                delete form.dataset.confirmado;
            }

            return;
        }

        if (carritoOcupado || !form.reportValidity()) return;

        carritoOcupado = true;

        let guardado = false;
        const deshabilitados = [];

        try {
            if (form.matches("[data-confirmar]") &&
                !await confirmar(form)) {
                return;
            }

            if (!form.isConnected) return;

            // Capturamos los datos antes de deshabilitar los controles.
            const datos = new FormData(form);

            if (event.submitter?.name) {
                datos.append(
                    event.submitter.name,
                    event.submitter.value
                );
            }

            // Evita envíos simultáneos desde esta página.
            for (const otro of document.forms) {
                if (!esFormularioCarrito(otro)) continue;

                for (const control of otro.querySelectorAll(
                    "button, input[type='number']"
                )) {
                    deshabilitados.push([
                        control,
                        control.disabled
                    ]);

                    control.disabled = true;
                }
            }

            const respuesta = await fetch(form.action, {
                method: "POST",
                body: datos,
                credentials: "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            const tipo =
                respuesta.headers.get("content-type") || "";

            if (respuesta.redirected ||
                !tipo.includes("application/json")) {
                throw new Error(
                    "Recargá la página para verificar tu sesión."
                );
            }

            const resultado = await respuesta.json();

            actualizarContador(resultado.unidades);

            if (!respuesta.ok) {
                await refrescarCarrito();

                notificar(
                    resultado.mensaje ||
                    "No se pudo modificar el carrito.",
                    "error"
                );

                return;
            }

            guardado = true;

            await refrescarCarrito();

            notificar(resultado.mensaje);
        } catch (error) {
            notificar(
                guardado
                    ? "El cambio se guardó. Recargá la página para ver el carrito actualizado."
                    : "No se pudo confirmar el resultado. Recargá antes de reintentar para evitar agregar unidades dos veces.",
                "error"
            );
        } finally {
            for (const [control, anterior] of deshabilitados) {
                if (control.isConnected) {
                    control.disabled = anterior;
                }
            }

            carritoOcupado = false;
        }
    });

    async function mostrarNotificaciones() {
        if (typeof Swal === "undefined") return;

        const tipos = [
            "success", "error", "warning", "info", "question"
        ];

        const titulos = {
            success: "Operación realizada",
            error: "No se pudo completar",
            warning: "Atención",
            info: "Información",
            question: "Consulta"
        };

        const mensajes = document.querySelectorAll(
            "[data-notificacion]:not([data-notificado])"
        );

        for (const elemento of mensajes) {
            const texto = elemento.textContent.trim();

            elemento.dataset.notificado = "true";

            if (!texto) continue;

            const tipo = tipos.includes(elemento.dataset.tipo)
                ? elemento.dataset.tipo
                : "info";

            elemento.hidden = true;

            await Swal.fire({
                icon: tipo,
                title: titulos[tipo],
                text: texto,
                confirmButtonText: "Aceptar"
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            mostrarNotificaciones
        );
    } else {
        mostrarNotificaciones();
    }
})();