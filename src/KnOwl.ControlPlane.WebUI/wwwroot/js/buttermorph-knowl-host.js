(function () {
    function openDesigner(url, title) {
        if (!window.ButterMorphHost) {
            window.location.href = url;
            return;
        }

        window.ButterMorphHost.openFrame(url, {
            title: title || "ButterMorph",
            width: 1280,
            height: 840
        });
    }

    function initializeOpenButtons() {
        document.querySelectorAll("[data-buttermorph-url]").forEach(function (button) {
            if (button.hasAttribute("data-buttermorph-payload-context")) {
                return;
            }

            button.addEventListener("click", function () {
                const url = button.getAttribute("data-buttermorph-url");
                const title = button.getAttribute("data-buttermorph-title") || "ButterMorph";
                if (url) {
                    openDesigner(url, title);
                }
            });
        });
    }

    async function loadDraftSchema(contextKey) {
        const response = await fetch("/Contracts/ButterMorphDrafts?handler=PayloadSchema&context=" + encodeURIComponent(contextKey), {
            cache: "no-store"
        });

        if (!response.ok) {
            return "";
        }

        const payload = await response.json();
        return payload.payloadSchemaJson || "";
    }

    function initializePayloadDesigner() {
        const buttons = document.querySelectorAll("[data-buttermorph-payload-context]");
        if (!buttons.length) {
            return;
        }

        buttons.forEach(function (button) {
            button.addEventListener("click", function () {
                const url = button.getAttribute("data-buttermorph-url");
                const title = button.getAttribute("data-buttermorph-title") || "Schema Designer";
                if (url) {
                    openDesigner(url, title);
                }
            });
        });

        window.addEventListener("message", async function (event) {
            if (event.origin !== window.location.origin || !event.data || event.data.type !== "ButterMorphPayloadSchemaDesignerSaved") {
                return;
            }

            const button = Array.from(buttons).find(function (candidate) {
                return candidate.getAttribute("data-buttermorph-payload-context") === event.data.contextKey;
            });
            if (!button) {
                return;
            }

            const contextKey = button.getAttribute("data-buttermorph-payload-context");
            const targetId = button.getAttribute("data-buttermorph-payload-target") || "payload-schema-json";
            const hiddenSchema = document.getElementById(targetId);
            const status = (button.closest(".event-schema-block") || document).querySelector("[data-buttermorph-payload-status]");
            const schema = await loadDraftSchema(contextKey);
            if (schema && hiddenSchema) {
                hiddenSchema.value = schema;
                if (status) {
                    status.textContent = "Schema captured from ButterMorph.";
                    status.classList.remove("text-danger");
                    status.classList.add("text-success");
                }
            }
        });

        const form = buttons[0].closest("form") || document.getElementById("event-editor-form");
        if (form) {
            form.addEventListener("submit", function (event) {
                const hiddenSchema = document.getElementById("payload-schema-json");
                const status = (document.querySelector("[data-buttermorph-payload-target='payload-schema-json']")?.closest(".event-schema-block") || document)
                    .querySelector("[data-buttermorph-payload-status]");
                if (!hiddenSchema || hiddenSchema.value.trim()) {
                    return;
                }

                event.preventDefault();
                if (status) {
                    status.textContent = "Capture the payload schema in ButterMorph before saving.";
                    status.classList.remove("text-success");
                    status.classList.add("text-danger");
                }
            });
        }
    }

    function initializeRedirectMessages() {
        const redirectNode = document.querySelector("[data-buttermorph-redirect-url]");
        const redirectUrl = redirectNode ? redirectNode.getAttribute("data-buttermorph-redirect-url") : "";
        const handlesEventCreate = document.querySelector("[data-buttermorph-event-create]");
        const handlesCommandCreate = document.querySelector("[data-buttermorph-command-create]");
        if (!redirectUrl && !handlesEventCreate && !handlesCommandCreate) {
            return;
        }

        window.addEventListener("message", async function (event) {
            if (event.origin !== window.location.origin || !event.data) {
                return;
            }

            const type = event.data.type || "";
            const contextKey = event.data.contextKey || "";
            if (type === "ButterMorphPayloadSchemaDesignerSaved" && contextKey.indexOf("event:new:") === 0) {
                const response = await fetch("/Contracts/ButterMorphDrafts?handler=CreatedEvent&context=" + encodeURIComponent(contextKey), {
                    cache: "no-store"
                });
                if (response.ok) {
                    const payload = await response.json();
                    if (payload.redirectUrl) {
                        window.location.href = payload.redirectUrl;
                    }
                }
                return;
            }

            if (type === "ButterMorphPayloadSchemaDesignerSaved" && contextKey.indexOf("command:new:") === 0) {
                const response = await fetch("/Contracts/ButterMorphDrafts?handler=CreatedCommand&context=" + encodeURIComponent(contextKey), {
                    cache: "no-store"
                });
                if (response.ok) {
                    const payload = await response.json();
                    if (payload.redirectUrl) {
                        window.location.href = payload.redirectUrl;
                    }
                }
                return;
            }

            if (type === "ButterMorphSchemaTypeDesignerSaved" ||
                type === "ButterMorphFieldMetadataDesignerSaved" ||
                (redirectUrl && type === "ButterMorphPayloadSchemaDesignerSaved")) {
                window.location.href = redirectUrl;
            }
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        initializeOpenButtons();
        initializePayloadDesigner();
        initializeRedirectMessages();
    });
}());
