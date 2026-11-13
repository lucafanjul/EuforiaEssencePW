(() => {
    "use strict";

    const abrir = document.getElementById("abrirAsistente");
    const cerrar = document.getElementById("cerrarAsistente");
    const panel = document.getElementById("panelAsistente");
    const formulario = document.getElementById("formAsistente");
    const mensajes = document.getElementById("mensajesAsistente");
    const entrada = document.getElementById("consultaAsistente");
    const enviar = document.getElementById("enviarAsistente");

    if (!abrir || !panel || !formulario) return;

    let ocupado = false;

    function cambiarVisibilidad(visible) {
        panel.hidden = !visible;
        abrir.setAttribute("aria-expanded", String(visible));

        if (visible) {
            entrada.focus();
        } else {
            abrir.focus();
        }
    }

    abrir.addEventListener("click", () => {
        cambiarVisibilidad(panel.hidden);
    });

    cerrar.addEventListener("click", () => {
        cambiarVisibilidad(false);
    });

    panel.addEventListener("keydown", (evento) => {
        if (evento.key === "Escape") {
            cambiarVisibilidad(false);
        }
    });

    function agregarMensaje(texto, clase) {
        const elemento = document.createElement("div");

        elemento.className = "ee-chat-mensaje " + clase;
        elemento.textContent = texto;

        mensajes.appendChild(elemento);
        mensajes.scrollTop = mensajes.scrollHeight;

        return elemento;
    }

    formulario.addEventListener("submit", async (evento) => {
        evento.preventDefault();

        const texto = entrada.value.trim();

        if (ocupado || !texto || !formulario.reportValidity()) return;

        ocupado = true;
        enviar.disabled = true;
        entrada.disabled = true;
        enviar.textContent = "Consultando…";

        agregarMensaje(texto, "ee-chat-consulta");

        const espera = agregarMensaje(
            "Consultando el catálogo…",
            "ee-chat-respuesta"
        );

        const datos = new FormData();

        datos.append("Mensaje", texto);
        datos.append(
            "__RequestVerificationToken",
            formulario.querySelector(
                'input[name="__RequestVerificationToken"]'
            ).value
        );

        try {
            const respuesta = await fetch(formulario.action, {
                method: "POST",
                body: datos
            });

            const tipo = respuesta.headers.get("content-type") || "";

            if (!tipo.includes("application/json")) {
                throw new Error(
                    "No se pudo enviar la consulta. Recargá la página."
                );
            }

            const resultado = await respuesta.json();

            if (!respuesta.ok) {
                throw new Error(
                    resultado.mensaje || "No se pudo completar la consulta."
                );
            }

            espera.textContent = resultado.respuesta;
            entrada.value = "";

            for (const producto of resultado.productos || []) {
                const destino = new URL(
                    producto.url,
                    window.location.origin
                );

                // Solo enlaces internos.
                if (destino.origin !== window.location.origin) continue;

                const enlace = document.createElement("a");
                enlace.className = "ee-chat-producto";
                enlace.href = destino.href;

                const nombre = document.createElement("strong");
                nombre.textContent = producto.nombre;

                const precio = document.createElement("span");
                precio.textContent = producto.precio;

                enlace.append(nombre, precio);
                espera.appendChild(enlace);
            }
        } catch (error) {
            espera.textContent = error.message ||
                "No se pudo conectar con el asistente.";
        } finally {
            ocupado = false;
            enviar.disabled = false;
            entrada.disabled = false;
            enviar.textContent = "Enviar";

            mensajes.scrollTop = mensajes.scrollHeight;

            if (!panel.hidden) entrada.focus();
        }
    });
})();