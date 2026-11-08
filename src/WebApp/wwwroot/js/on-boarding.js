(() => {
    "use strict";

    // Ubah ke 5 (dan tambahkan atribut `multiple` di input) saat sudah multi-file.
    // Model server juga harus jadi List<IFormFile>.
    const MAX_FILES = 1;
    const MAX_BYTES = 5 * 1024 * 1024;
    const ALLOWED = ["image/jpeg", "image/png", "image/webp"];
    const SINGLE = MAX_FILES === 1;

    const form = document.getElementById("onboardingForm");
    if (!form) return;

    const $ = (selector, root = document) => root.querySelector(selector);
    const lower = s => String(s).toLowerCase();
    const make = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text != null) node.textContent = text;
        return node;
    };

    const essay = document.getElementById("Essay");
    const essayCard = $("[data-ob-essay-card]", form);
    const scoreInput = $('[name="CreateStudentProfileRequest.FinalScore"]', form);

    const ICON_DOC = '<svg class="h-5 w-5" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.8" viewBox="0 0 24 24" aria-hidden="true"><path d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" stroke-linecap="round"></path></svg>';
    const ICON_TRASH = '<svg class="h-5 w-5" width="20" height="20" fill="none" stroke="currentColor" stroke-width="1.6" viewBox="0 0 24 24" aria-hidden="true"><path d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" stroke-linecap="round"></path></svg>';

    // =========================================================
    // Error helper
    // =========================================================
    const errorEl = key => form.querySelector(`[data-ob-error="${CSS.escape(key)}"]`);

    function clearErrors() {
        form.querySelectorAll("[data-ob-error]").forEach(el => (el.textContent = ""));
        form.querySelectorAll(".ob-invalid").forEach(el => el.classList.remove("ob-invalid"));
    }

    function setError(key, message, invalidEl) {
        const el = errorEl(key) ?? errorEl("general");
        if (el) el.textContent = el.textContent ? `${el.textContent} ${message}` : message;
        invalidEl?.classList.add("ob-invalid");
    }

    // =========================================================
    // Stepper
    // =========================================================
    function goTo(step) {
        form.querySelectorAll("[data-ob-step]").forEach(s => (s.hidden = Number(s.dataset.obStep) !== step));
        const result = step === 3;
        ["profile-header", "profile-progress", "profile-footer"].forEach(part => {
            $(`[data-ob-${part}]`).hidden = result;
        });
        $("[data-ob-result-header]").hidden = !result;
        $("[data-ob-page-title]").textContent = step === 2 ? "Unggah Sertifikat" : "Profil Beasiswa";
        $("[data-ob-step-label]").textContent = `LANGKAH ${Math.min(step, 2)} DARI 2`;
        $("[data-ob-step-name]").textContent = step === 1 ? "Esai Diri" : "Sertifikat";
        const fill = $("[data-ob-progress-bar]");
        fill.style.width = step === 1 ? "50%" : "100%";
        fill.parentElement.setAttribute("aria-valuenow", step === 1 ? "50" : "100");
        closeDropdown();
        if (!result) form.querySelector(`[data-ob-step="${step}"] h1`)?.focus({ preventScroll: true });
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    // =========================================================
    // Step 1: IPK + essay + chips
    // =========================================================
    const counter = $("[data-ob-essay-count]");
    counter.textContent = essay.value.length;
    essay.addEventListener("input", () => {
        counter.textContent = essay.value.length;
        essayCard.classList.remove("ob-invalid");
        errorEl("Essay").textContent = "";
    });
    scoreInput?.addEventListener("input", () => {
        scoreInput.classList.remove("ob-invalid");
        errorEl("FinalScore").textContent = "";
    });

    const chipField = $("[data-ob-chips]");
    const chipBox = $("[data-ob-chips-box]", chipField);
    const chipOptions = (() => {
        try { return JSON.parse(chipField.dataset.options || "[]").map(o => ({ value: String(o.id), label: o.name })); }
        catch { return []; }
    })();

    const chips = []; // { value, label }
    let dd = null, ddItems = [], ddActive = -1;

    const chipInput = document.createElement("input");
    chipInput.type = "text";
    chipInput.id = "obChipInput";
    chipInput.className = "ob-chip-input";
    chipInput.autocomplete = "off";
    chipInput.setAttribute("role", "combobox");
    chipInput.setAttribute("aria-expanded", "false");
    chipBox.appendChild(chipInput);

    function renderChips() {
        chipBox.querySelectorAll(".ob-chip").forEach(c => c.remove());

        chips.forEach(chip => {
            const el = make("span", "ob-chip");
            const remove = make("button", "ob-chip-remove", "\u00d7");
            remove.type = "button";
            remove.setAttribute("aria-label", `Hapus ${chip.label}`);
            remove.addEventListener("click", () => removeChip(chip.value));
            el.append(make("span", null, chip.label), remove);
            chipInput.before(el);
        });

        chipInput.placeholder = chips.length ? "" : (chipField.dataset.placeholder || "");
        $("[data-ob-category-count]").textContent = chips.length;
    }

    function addChip(option) {
        if (chips.some(c => c.value === option.value)) return;
        chips.push(option);
        chipBox.classList.remove("ob-invalid");
        errorEl("CategoryIds").textContent = "";
        renderChips();
    }

    function removeChip(value) {
        const i = chips.findIndex(c => c.value === value);
        if (i >= 0) chips.splice(i, 1);
        renderChips();
        chipInput.focus();
        if (dd) renderDropdown(false);
    }

    function positionDropdown() {
        if (!dd) return;
        const rect = chipBox.getBoundingClientRect();
        const vv = window.visualViewport;
        const vTop = vv ? vv.offsetTop : 0;
        const vHeight = vv ? vv.height : window.innerHeight;
        const height = Math.min(dd.scrollHeight, 220) + 8;
        const below = vTop + vHeight - rect.bottom;
        const above = rect.top - vTop;

        dd.style.left = `${rect.left}px`;
        dd.style.width = `${rect.width}px`;

        if (below < height && above > below) {
            dd.style.top = "auto";
            dd.style.bottom = `${window.innerHeight - rect.top + 2}px`;
        } else {
            dd.style.bottom = "auto";
            dd.style.top = `${rect.bottom + 2}px`;
        }
    }

    function renderDropdown(resetActive = true) {
        const q = lower(chipInput.value.trim());
        const picked = new Set(chips.map(c => c.value));

        ddItems = chipOptions
            .filter(o => !picked.has(o.value) && (!q || lower(o.label).includes(q)))
            .sort((a, b) => (lower(a.label).startsWith(q) ? 0 : 1) - (lower(b.label).startsWith(q) ? 0 : 1));

        if (resetActive || ddActive >= ddItems.length) ddActive = ddItems.length ? 0 : -1;

        if (!dd) {
            dd = make("div", "ob-dd");
            dd.setAttribute("role", "listbox");
            // mousedown dicegah supaya input tidak kehilangan fokus
            dd.addEventListener("mousedown", e => e.preventDefault());
            document.body.appendChild(dd);
            chipInput.setAttribute("aria-expanded", "true");
        }

        dd.replaceChildren();

        if (!ddItems.length) {
            dd.append(make("div", "ob-dd-empty", q ? "Tidak ada pilihan yang cocok" : "Tidak ada pilihan"));
        }

        ddItems.forEach((item, i) => {
            const btn = make("button", "ob-dd-item" + (i === ddActive ? " active" : ""), item.label);
            btn.type = "button";
            btn.setAttribute("role", "option");
            btn.addEventListener("click", () => selectItem(i));
            dd.appendChild(btn);
        });

        positionDropdown();
        $(".active", dd)?.scrollIntoView({ block: "nearest" });
    }

    function closeDropdown() {
        dd?.remove();
        dd = null;
        ddItems = [];
        chipInput.setAttribute("aria-expanded", "false");
    }

    function selectItem(i) {
        const item = ddItems[i];
        if (!item) return;
        chipInput.value = "";
        addChip(item);
        closeDropdown();
        chipInput.focus();
        renderDropdown();
    }

    function moveActive(step) {
        if (!ddItems.length) return;
        ddActive = (ddActive + step + ddItems.length) % ddItems.length;
        dd.querySelectorAll(".ob-dd-item").forEach((el, i) => el.classList.toggle("active", i === ddActive));
        $(".active", dd)?.scrollIntoView({ block: "nearest" });
    }

    chipBox.addEventListener("click", e => {
        if (e.target.closest(".ob-chip")) return;
        chipInput.focus();
    });
    chipInput.addEventListener("focus", () => renderDropdown());
    chipInput.addEventListener("input", () => renderDropdown());
    chipInput.addEventListener("blur", () => {
        chipInput.value = ""; // hanya pilihan dari daftar yang boleh jadi chip
        closeDropdown();
    });
    chipInput.addEventListener("keydown", e => {
        if (e.isComposing) return;

        if (e.key === "ArrowDown" || e.key === "ArrowUp") {
            e.preventDefault();
            if (!dd) renderDropdown();
            else moveActive(e.key === "ArrowDown" ? 1 : -1);
        } else if (e.key === "Enter") {
            e.preventDefault(); // jangan submit form
            if (dd && ddActive >= 0) selectItem(ddActive);
        } else if (e.key === "Escape") {
            closeDropdown();
        } else if (e.key === "Backspace" && !chipInput.value && chips.length) {
            chips.pop();
            renderChips();
            if (dd) renderDropdown(false);
        }
    });
    window.addEventListener("resize", positionDropdown);
    window.addEventListener("scroll", positionDropdown, true);
    window.visualViewport?.addEventListener("resize", positionDropdown);

    renderChips();

    function validateStep1() {
        clearErrors();
        let firstFocus = null;
        const fail = (key, message, box, focusEl) => {
            setError(key, message, box);
            firstFocus ??= focusEl;
        };

        const score = scoreInput?.value.trim() ?? "";
        if (score && !/^\d+([.,]\d+)?$/.test(score)) {
            fail("FinalScore", "IPK harus berupa angka, contoh 3.75.", scoreInput, scoreInput);
        }

        const text = essay.value.trim();
        if (!text) {
            fail("Essay", "Essay Bebas wajib diisi.", essayCard, essay);
        } else if (text.length < 20 || text.length > 500) {
            fail("Essay", "Essay Bebas harus terdiri dari 20 sampai 500 karakter.", essayCard, essay);
        }

        if (!chips.length) {
            fail("CategoryIds", "Pilih minimal satu kategori.", chipBox, chipInput);
        }

        firstFocus?.focus();
        return !firstFocus;
    }

    $("[data-ob-next]", form).addEventListener("click", () => {
        if (validateStep1()) goTo(2);
    });
    $("[data-ob-back]", form).addEventListener("click", () => goTo(1));
    $("[data-ob-header-back]").addEventListener("click", e => {
        if (!$('[data-ob-step="2"]', form).hidden) {
            e.preventDefault();
            goTo(1);
        }
    });

    // =========================================================
    // Step 2: file sertifikat (1 file; siap multi lewat MAX_FILES)
    // =========================================================
    const fileInput = document.getElementById("certificateInput");
    const dropzone = $("[data-ob-dropzone]", form);
    const fileList = $("[data-ob-file-list]", form);
    const files = [];
    let submitting = false;

    const sameFile = (a, b) => a.name === b.name && a.size === b.size && a.lastModified === b.lastModified;

    function selectFiles(selectedFiles) {
        errorEl("Certificates").textContent = "";
        dropzone.classList.remove("ob-invalid");
        const rejected = [];

        [...selectedFiles].forEach(f => {
            if (files.some(x => sameFile(x, f))) return;
            if (!ALLOWED.includes(f.type)) { rejected.push(`${f.name}: hanya JPG, PNG, atau WebP.`); return; }
            if (f.size > MAX_BYTES) { rejected.push(`${f.name}: maksimal ${MAX_BYTES / 1024 / 1024} MB.`); return; }

            if (SINGLE) {
                files.length = 0; // mode 1 file: file baru menggantikan yang lama
            } else if (files.length >= MAX_FILES) {
                rejected.push(`Maksimal ${MAX_FILES} sertifikat.`);
                return;
            }

            files.push(f);
        });

        fileInput.value = ""; // supaya file yang sama bisa dipilih ulang setelah dihapus
        if (rejected.length) setError("Certificates", [...new Set(rejected)].join(" "), dropzone);
        renderFiles();
    }

    fileInput.addEventListener("change", () => selectFiles(fileInput.files));
    ["dragenter", "dragover"].forEach(event => dropzone.addEventListener(event, e => {
        e.preventDefault();
        dropzone.classList.add("ob-dragging");
    }));
    dropzone.addEventListener("dragleave", e => {
        if (!dropzone.contains(e.relatedTarget)) dropzone.classList.remove("ob-dragging");
    });
    dropzone.addEventListener("drop", e => {
        e.preventDefault();
        dropzone.classList.remove("ob-dragging");
        if (!submitting && e.dataTransfer?.files) selectFiles(e.dataTransfer.files);
    });

    function renderFiles() {
        fileList.replaceChildren();
        $("[data-ob-file-count]", form).textContent = files.length;

        files.forEach((f, i) => {
            const li = make("li", "ob-file");

            const icon = make("div", "ob-file-icon");
            icon.innerHTML = ICON_DOC;

            const size = (f.size / 1024 / 1024).toLocaleString("id-ID", { maximumFractionDigits: 2 });
            const text = make("div", "ob-file-text");
            text.append(make("p", "ob-file-name", f.name), make("p", "ob-file-meta", `${size} MB • Siap dianalisis`));

            const main = make("div", "ob-file-main");
            main.append(icon, text);

            const remove = make("button", "ob-file-remove");
            remove.type = "button";
            remove.setAttribute("aria-label", `Hapus ${f.name}`);
            remove.innerHTML = ICON_TRASH;
            remove.addEventListener("click", () => { files.splice(i, 1); renderFiles(); });

            li.append(main, remove);
            fileList.appendChild(li);
        });
    }

    // =========================================================
    // Submit
    // =========================================================
    form.addEventListener("submit", async e => {
        e.preventDefault();
        if (submitting) return;

        if (!validateStep1()) { goTo(1); return; }

        clearErrors();
        if (!files.length) {
            setError("Certificates", "Unggah minimal satu sertifikat.", dropzone);
            return;
        }

        const data = new FormData(form); // Essay + IPK + antiforgery token
        if (scoreInput) data.set(scoreInput.name, scoreInput.value.trim().replace(",", ".")); // model binding pakai titik
        // Server menimpa Description dengan Essay; dikirim juga agar validasi [Required] di model profil tidak gagal.
        data.set("CreateStudentProfileRequest.Description", essay.value.trim());
        chips.forEach((c, i) => data.append(`CategoryIds[${i}]`, c.value));
        files.forEach(f => data.append("Certificates", f)); // key sama cocok untuk IFormFile maupun List<IFormFile>

        const button = $("[data-ob-submit]", form);
        const label = button.innerHTML;
        submitting = true;
        button.disabled = true;
        button.textContent = "Menganalisa...";

        try {
            const res = await fetch(form.action, {
                method: "POST",
                body: data,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (res.redirected || res.status === 401 || res.status === 403) {
                window.location.assign("/identity/login?ReturnUrl=" + encodeURIComponent(location.pathname + location.search));
                return;
            }

            if (res.ok) {
                const payload = await res.json().catch(() => null);
                if (!payload) throw new Error("Respons server tidak valid.");
                showResult(payload);
                goTo(3);
                return;
            }

            if (res.status === 422) {
                showServerErrors((await res.json()).errors);
                return;
            }

            throw new Error(`HTTP ${res.status}`);
        } catch (err) {
            console.error("[onboarding]", err);
            setError("general", "Terjadi kesalahan. Silakan coba lagi.");
        } finally {
            submitting = false;
            button.disabled = false;
            button.innerHTML = label;
        }
    });

    function showServerErrors(errors) {
        const invalidEl = { Essay: essayCard, CategoryIds: chipBox, Certificates: dropzone, FinalScore: scoreInput };
        const step1Targets = ["Essay", "CategoryIds", "FinalScore", "profile"];
        let firstStep = 2;

        Object.entries(errors || {}).forEach(([key, messages]) => {
            const base = key.replace(/\[\d+\]/g, "").replace(/^request\./i, "");
            let target;
            if (base === "FinalScore" || base.endsWith(".FinalScore")) target = "FinalScore";
            else if (["Essay", "CategoryIds", "Certificates"].includes(base)) target = base;
            else target = base.startsWith("CreateStudentProfileRequest") ? "profile" : "general";

            if (step1Targets.includes(target)) firstStep = 1;
            setError(target, [...new Set(messages)].join(" "), invalidEl[target]);
        });

        goTo(firstStep);
    }

    // =========================================================
    // Hasil rekomendasi
    // =========================================================
    function showResult(payload) {
        const box = $("[data-ob-result]", form);
        box.replaceChildren();

        let recs = [];
        try { recs = JSON.parse(payload?.result || "{}").recommendations ?? []; }
        catch { /* output AI bukan JSON valid */ }

        if (!recs.length) {
            box.appendChild(make("div", "ob-empty",
                "Belum ada beasiswa yang cocok dengan pilihanmu. Coba pilih kategori lain."));
            return;
        }

        const scholarships = new Map((payload?.scholarships ?? []).map(s => [lower(s.id), s]));
        recs.forEach(r => box.appendChild(recommendationCard(r, scholarships.get(lower(r.opportunityId ?? "")))));
    }

    function recommendationCard(r, scholarship) {
        const score = Math.max(0, Math.min(100, Number(r.matchScore) || 0));
        const tone = score >= 80 ? "high" : score >= 60 ? "mid" : "low";

        const card = make("article", `ob-rec ob-tone-${tone}`);

        const head = make("div", "ob-rec-head");
        head.append(
            make("h3", "ob-rec-title", scholarship?.name || r.opportunityName || "Beasiswa"),
            make("span", "ob-badge", `${score}%`));
        card.append(head);

        if (scholarship) {
            const overview = make("div", "ob-overview");
            const imageUrl = safeUrl(scholarship.contentImageUrl);
            if (imageUrl) {
                const image = make("img", "ob-thumb");
                image.src = imageUrl;
                image.alt = `Poster ${scholarship.name}`;
                image.loading = "lazy";
                overview.append(image);
            }
            const info = make("div", "ob-grow");
            info.append(
                categoryPills(scholarship.categories),
                make("p", "ob-text ob-clamp", scholarship.description),
                make("p", "ob-strong", prizeText(scholarship.totalPrize)),
                make("p", "ob-text", `Pendaftaran: ${dateText(scholarship.registrationStartAt)} – ${dateText(scholarship.registrationEndAt)}`));
            overview.append(info);
            card.append(overview);
        }

        const bar = make("div", "ob-bar");
        bar.setAttribute("role", "progressbar");
        bar.setAttribute("aria-valuenow", score);
        bar.setAttribute("aria-valuemin", 0);
        bar.setAttribute("aria-valuemax", 100);
        const fill = make("span");
        fill.style.width = `${score}%`;
        bar.append(fill);

        const note = make("div", "ob-note");
        note.append(make("strong", null, "Saran: "), document.createTextNode(r.feedback ?? ""));

        const basis = make("details", "ob-details");
        basis.append(make("summary", null, "Dasar perhitungan skor"), make("p", null, r.matchScoreSource));

        card.append(make("p", "ob-label", "Kecocokan dengan profilmu"), bar, make("p", "ob-text", r.matchReason), note, basis);

        if (scholarship) {
            const actions = make("div", "ob-actions");
            const detail = make("button", "ob-btn", "Lihat detail beasiswa");
            detail.type = "button";
            detail.addEventListener("click", () => openModal(scholarship));
            actions.append(detail);
            const registration = externalLink(scholarship.registrationUrl, "Daftar beasiswa", "ob-btn ob-btn-primary");
            if (registration) actions.append(registration);
            card.append(actions);
        } else {
            card.append(make("p", "ob-text", "Detail beasiswa ini belum tersedia."));
        }
        return card;
    }

    function safeUrl(value) {
        if (!value) return null;
        try {
            const url = new URL(value);
            return ["http:", "https:"].includes(url.protocol) ? url.href : null;
        } catch { return null; }
    }

    function externalLink(url, label, classes) {
        const href = safeUrl(url);
        if (!href) return null;
        const link = make("a", classes, label);
        link.href = href;
        link.target = "_blank";
        link.rel = "noopener noreferrer";
        return link;
    }

    function dateText(value) {
        const date = new Date(value);
        return !value || !Number.isFinite(date.getTime()) || date.getUTCFullYear() <= 1
            ? "Belum diinformasikan"
            : new Intl.DateTimeFormat("id-ID", { dateStyle: "long", timeStyle: "short", timeZone: "Asia/Jakarta" }).format(date) + " WIB";
    }

    function prizeText(value) {
        return Number(value) > 0
            ? new Intl.NumberFormat("id-ID", { style: "currency", currency: "IDR", maximumFractionDigits: 0 }).format(value)
            : "Nominal bantuan belum diinformasikan";
    }

    function categoryPills(categories) {
        const pills = make("div", "ob-pills");
        (categories ?? []).forEach(name => pills.append(make("span", "ob-pill", name)));
        return pills;
    }

    // =========================================================
    // Modal detail beasiswa
    // =========================================================
    const modal = document.getElementById("obScholarshipModal");
    let lastFocus = null;

    function openModal(scholarship) {
        populateScholarshipModal(scholarship);
        lastFocus = document.activeElement;
        modal.hidden = false;
        document.body.style.overflow = "hidden";
        $(".ob-modal-close", modal).focus();
    }

    function closeModal() {
        if (modal.hidden) return;
        modal.hidden = true;
        document.body.style.overflow = "";
        lastFocus?.focus?.();
    }

    modal.querySelectorAll("[data-ob-modal-close]").forEach(el => el.addEventListener("click", closeModal));
    document.addEventListener("keydown", e => { if (e.key === "Escape") closeModal(); });

    function populateScholarshipModal(s) {
        $(".ob-modal-title", modal).textContent = s.name;
        const body = $("[data-ob-modal-body]", modal);
        body.replaceChildren();

        const imageUrl = safeUrl(s.contentImageUrl);
        if (imageUrl) {
            const link = externalLink(imageUrl, null, "");
            const image = make("img", "ob-modal-img");
            image.src = imageUrl;
            image.alt = `Poster ${s.name}`;
            link.append(image);
            body.append(link);
        }

        body.append(categoryPills(s.categories));

        const about = make("section", "ob-sec");
        about.append(make("h3", null, "Tentang beasiswa"), make("p", null, s.description || "Belum diinformasikan."));
        body.append(about);

        [["Persyaratan", s.requirement], ["Kontak", s.contactPerson]].forEach(([title, items]) => {
            const section = make("section", "ob-sec");
            section.append(make("h3", null, title));
            if (items?.length) {
                const list = make("ul");
                items.forEach(item => list.append(make("li", null, item)));
                section.append(list);
            } else section.append(make("p", null, "Belum diinformasikan."));
            body.append(section);
        });

        const facts = make("dl", "ob-facts");
        [["Bantuan / total hadiah", prizeText(s.totalPrize)],
            ["Pendaftaran dibuka", dateText(s.registrationStartAt)],
            ["Pendaftaran ditutup", dateText(s.registrationEndAt)]].forEach(([label, value]) => {
            facts.append(make("dt", null, label), make("dd", null, value));
        });
        body.append(facts);

        [["guide", s.guideUrl], ["register", s.registrationUrl]].forEach(([key, value]) => {
            const link = $(`[data-ob-modal-${key}]`, modal);
            const url = safeUrl(value);
            link.hidden = !url;
            if (url) link.href = url;
            else link.removeAttribute("href");
        });
    }
})();