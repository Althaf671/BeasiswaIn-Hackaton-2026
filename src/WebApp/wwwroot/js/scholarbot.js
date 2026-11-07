(() => {
    "use strict";

    const root = document.querySelector("[data-scholarbot]");
    if (!root) return;

    const form = root.querySelector("[data-sb-form]");
    const input = root.querySelector("[data-sb-input]");
    const send = root.querySelector("[data-sb-send]");
    const reset = root.querySelector("[data-sb-reset]");
    const count = root.querySelector("[data-sb-count]");
    const status = root.querySelector("[data-sb-status]");
    const errorBox = root.querySelector("[data-sb-error]");
    const errorText = root.querySelector("[data-sb-error-text]");
    const loginLink = root.querySelector("[data-sb-login]");
    const transcript = root.querySelector("[data-sb-transcript]");
    const conversation = root.querySelector("[data-sb-conversation]");
    const welcome = root.querySelector("[data-sb-welcome]");
    const prompts = Array.from(root.querySelectorAll("[data-sb-prompt]"));
    const encoder = new TextEncoder();
    const dateFormat = new Intl.DateTimeFormat("id-ID", { dateStyle: "long" });
    const maxMessageCharacters = Number(root.dataset.maxMessage);
    const maxContextCharacters = Number(root.dataset.maxContext);
    const maxHistoryMessages = Number(root.dataset.maxHistory);
    let history = [];
    let pending = false;

    function element(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    }

    function updateComposer() {
        count.textContent = input.value.length.toLocaleString("id-ID");
        send.disabled = pending || !input.value.trim() || input.value.length > maxMessageCharacters;
    }

    function setPending(value) {
        pending = value;
        input.readOnly = value;
        reset.disabled = value;
        prompts.forEach(button => { button.disabled = value; });
        send.textContent = value ? "Memproses…" : "Kirim ↑";
        form.setAttribute("aria-busy", String(value));
        updateComposer();
    }

    function clearError() {
        errorBox.hidden = true;
        errorText.textContent = "";
        loginLink.hidden = true;
    }

    function showError(message, needsLogin = false) {
        errorText.textContent = message;
        loginLink.hidden = !needsLogin;
        errorBox.hidden = false;
    }

    function scrollToLatest() {
        conversation.scrollTop = conversation.scrollHeight;
    }

    function addMessage(role, text) {
        const article = element("article", `sb-message sb-message--${role}`);
        article.append(element("h3", "sb-message-author", role === "user" ? "Kamu" : "ScholarBot"));
        article.append(element("div", "sb-message-text", text));
        transcript.append(article);
        return article;
    }

    function safeUrl(value) {
        if (typeof value !== "string") return null;
        try {
            const url = new URL(value);
            return url.protocol === "https:" || url.protocol === "http:" ? url.href : null;
        } catch {
            return null;
        }
    }

    function addSourceLink(container, label, value) {
        const url = safeUrl(value);
        if (!url) return false;
        const link = element("a", "sb-source-link", label);
        link.href = url;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        container.append(link);
        return true;
    }

    function addSources(article, sources) {
        if (!Array.isArray(sources) || sources.length === 0) return;
        const details = element("details", "sb-sources");
        details.open = true;
        details.append(element("summary", "", `Sumber beasiswa · ${sources.length}`));
        const cards = element("div", "sb-source-grid");
        for (const source of sources) {
            if (!source || typeof source !== "object") continue;
            const card = element("section", "sb-source-card");
            card.append(element("h4", "", typeof source.name === "string" ? source.name : "Beasiswa"));
            if (typeof source.id === "string" && source.id) {
                card.append(element("p", "sb-source-id", `ID: ${source.id}`));
            }
            const deadline = typeof source.deadline === "string" ? new Date(source.deadline) : null;
            if (deadline && !Number.isNaN(deadline.getTime())) {
                card.append(element("p", "sb-source-deadline", `Batas pendaftaran: ${dateFormat.format(deadline)}`));
            }
            const links = element("div", "sb-source-links");
            const registration = addSourceLink(links, "Pendaftaran ↗", source.registrationUrl);
            const guide = addSourceLink(links, "Panduan ↗", source.guideUrl);
            if (!registration && !guide) links.append(element("span", "", "Tautan resmi tidak tersedia."));
            card.append(links);
            cards.append(card);
        }
        details.append(cards);
        article.append(details);
    }

    function nonnegativeNumber(value) {
        return typeof value === "number" && Number.isFinite(value) && value >= 0;
    }

    function addTraceAndUsage(article, traces, usage) {
        const actualTraces = Array.isArray(traces) ? traces : [];
        if (actualTraces.length === 0 && !usage) return;
        const details = element("details", "sb-trace");
        details.append(element("summary", "", "Aktivitas & penggunaan"));
        if (actualTraces.length > 0) {
            const list = element("ul", "sb-trace-list");
            for (const trace of actualTraces) {
                if (!trace || typeof trace.name !== "string") continue;
                const duration = nonnegativeNumber(trace.durationMilliseconds)
                    ? ` · ${trace.durationMilliseconds.toLocaleString("id-ID")} ms` : "";
                list.append(element("li", "", `${trace.name} · ${trace.success === true ? "berhasil" : "tidak berhasil"}${duration}`));
            }
            details.append(list);
        }
        if (usage && typeof usage === "object") {
            const stats = [];
            if (nonnegativeNumber(usage.inputTokens)) stats.push(`${usage.inputTokens.toLocaleString("id-ID")} token masuk`);
            if (nonnegativeNumber(usage.outputTokens)) stats.push(`${usage.outputTokens.toLocaleString("id-ID")} token keluar`);
            if (nonnegativeNumber(usage.apiCalls)) stats.push(`${usage.apiCalls.toLocaleString("id-ID")} panggilan AI`);
            if (stats.length > 0) details.append(element("p", "sb-usage", stats.join(" · ")));
            const cost = nonnegativeNumber(usage.costUsd)
                ? `Biaya dilaporkan penyedia: US$ ${usage.costUsd.toLocaleString("id-ID", { maximumSignificantDigits: 6 })}`
                : "Biaya tidak dilaporkan penyedia; bukan berarti gratis.";
            details.append(element("p", "sb-usage", cost));
        }
        article.append(details);
    }

    // Keep only complete user/assistant pairs; never include the pending message twice.
    function boundedHistory(entries, currentMessage) {
        const bounded = entries.map(entry => ({ role: entry.role, content: entry.content }));
        const totalCharacters = () => bounded.reduce((total, entry) => total + entry.content.length, currentMessage.length);
        const payloadBytes = () => encoder.encode(JSON.stringify({ message: currentMessage, history: bounded })).length;
        while (bounded.length > 0 && (bounded.length > maxHistoryMessages || totalCharacters() > maxContextCharacters || payloadBytes() > 250000)) {
            bounded.splice(0, 2);
        }
        return bounded;
    }

    function responseError(response) {
        const errors = {
            400: "Pesan atau konteks tidak valid, atau token keamanan halaman kedaluwarsa. Coba pesan lebih singkat. Jika berulang, salin drafmu lalu muat ulang halaman.",
            401: "Sesi masukmu berakhir. Masuk lagi di tab baru, lalu coba kirim ulang. Draf tetap ada di sini.",
            403: "Akses ditolak atau sesi keamanan kedaluwarsa. Masuk lagi; jika masih gagal, salin draf lalu muat ulang halaman.",
            408: "Waktu pemrosesan habis. Coba pertanyaan lebih singkat atau kirim ulang sebentar lagi.",
            413: "Pesan terlalu besar. Persingkat pesan, atau mulai percakapan baru setelah menyalin drafmu.",
            429: "Terlalu banyak permintaan. Tunggu sekitar satu menit, lalu coba kirim lagi.",
            503: "Layanan AI belum tersedia atau sedang sibuk. Coba lagi beberapa saat lagi.",
            504: "Layanan terlalu lama merespons. Coba pertanyaan lebih singkat atau kirim ulang sebentar lagi."
        };
        return errors[response.status] || "Jawaban belum bisa diproses. Coba lagi beberapa saat lagi; drafmu tetap tersimpan di halaman ini.";
    }

    input.addEventListener("input", updateComposer);
    input.addEventListener("keydown", event => {
        if (event.key === "Enter" && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            if (!pending && input.value.trim()) form.requestSubmit();
        }
    });

    prompts.forEach(button => {
        button.addEventListener("click", () => {
            if (pending) return;
            input.value = button.dataset.sbPrompt;
            clearError();
            updateComposer();
            input.focus();
        });
    });

    reset.addEventListener("click", () => {
        if (pending) return;
        history = [];
        transcript.replaceChildren();
        welcome.hidden = false;
        input.value = "";
        clearError();
        updateComposer();
        status.textContent = "Percakapan baru siap. Konteks chat dihapus; data akun tidak berubah.";
        conversation.scrollTop = 0;
        input.focus();
    });

    form.addEventListener("submit", async event => {
        event.preventDefault();
        const message = input.value.trim();
        if (pending || !message || input.value.length > maxMessageCharacters) return;
        const token = form.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            showError("Token keamanan tidak tersedia. Salin drafmu lalu muat ulang halaman sebelum mengirim.");
            return;
        }

        const requestHistory = boundedHistory(history, message);
        const controller = new AbortController();
        const timeout = window.setTimeout(() => controller.abort(), 105000);
        let succeeded = false;
        clearError();
        setPending(true);
        welcome.hidden = true;
        const userMessage = addMessage("user", message);
        userMessage.classList.add("sb-message--pending");
        status.textContent = "ScholarBot sedang membaca dan menyiapkan jawaban. Proses dapat memerlukan hingga 90 detik…";
        scrollToLatest();

        try {
            const endpoint = new URL(form.action, window.location.href);
            endpoint.searchParams.set("handler", "Chat");
            const response = await fetch(endpoint, {
                method: "POST",
                credentials: "same-origin",
                headers: {
                    "Content-Type": "application/json",
                    "Accept": "application/json",
                    "X-CSRF-TOKEN": token,
                    "X-Requested-With": "XMLHttpRequest"
                },
                body: JSON.stringify({ message, history: requestHistory }),
                signal: controller.signal
            });
            if (response.redirected || response.status === 401) {
                showError(responseError({ status: 401 }), true);
                return;
            }
            if (!response.ok) {
                showError(responseError(response), response.status === 403);
                return;
            }
            const data = await response.json();
            if (!data || typeof data.answer !== "string" || !data.answer.trim()) {
                showError("Jawaban yang diterima tidak valid. Coba kirim ulang; drafmu belum dihapus.");
                return;
            }

            userMessage.classList.remove("sb-message--pending");
            const answer = addMessage("assistant", data.answer);
            addSources(answer, data.sources);
            addTraceAndUsage(answer, data.toolCalls, data.usage);
            history = boundedHistory([
                ...requestHistory,
                { role: "user", content: message },
                { role: "assistant", content: data.answer }
            ], "");
            input.value = "";
            succeeded = true;
            status.textContent = "Jawaban tersedia. Periksa sumber dan persyaratan resmi sebelum mengambil keputusan.";
            scrollToLatest();
        } catch (error) {
            if (controller.signal.aborted) {
                showError("Koneksi melewati batas waktu. Coba pertanyaan lebih singkat atau kirim lagi sebentar lagi. Drafmu tetap ada.");
            } else if (error instanceof SyntaxError) {
                showError("Respons layanan tidak terbaca. Coba lagi; jika berulang, salin draf lalu muat ulang halaman.");
            } else {
                showError("Tidak dapat terhubung ke layanan. Periksa koneksi internet, lalu coba kirim ulang. Drafmu tetap ada.");
            }
        } finally {
            window.clearTimeout(timeout);
            if (!succeeded) {
                userMessage.remove();
                welcome.hidden = transcript.childElementCount > 0;
                status.textContent = "Pesan belum berhasil. Draf dan konteks sebelumnya tidak berubah.";
            }
            setPending(false);
            input.focus({ preventScroll: true });
        }
    });

    updateComposer();
})();
