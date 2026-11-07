(() => {
    // Ubah ke 5 (dan tambahkan atribut `multiple` di input) saat sudah multi-file.
    // Model server juga harus jadi List<IFormFile>.
    const MAX_FILES = 1;
    const MAX_BYTES = 5 * 1024 * 1024;
    const ALLOWED = ["image/jpeg", "image/png", "image/webp"];
    const SINGLE = MAX_FILES === 1;

    const form = document.getElementById("onboardingForm");
    if (!form) return;
    const essay = document.getElementById("Essay");
    const lower = s => String(s).toLowerCase();
    const icons = root => { if (typeof lucide !== "undefined") lucide.createIcons({ root }); };

    // =========================================================
    // Error helper
    // =========================================================
    const errorEl = key => form.querySelector(`[data-ob-error="${CSS.escape(key)}"]`);

    function clearErrors() {
        form.querySelectorAll("[data-ob-error]").forEach(el => (el.textContent = ""));
        form.querySelectorAll(".is-invalid").forEach(el => el.classList.remove("is-invalid"));
    }

    function setError(key, message, invalidEl) {
        const el = errorEl(key) ?? errorEl("general");
        if (el) el.textContent = el.textContent ? `${el.textContent} ${message}` : message;
        invalidEl?.classList.add("is-invalid");
    }

    // =========================================================
    // Stepper
    // =========================================================
    function goTo(step) {
        form.querySelectorAll("[data-ob-step]").forEach(s => (s.hidden = Number(s.dataset.obStep) !== step));
        const result = step === 3;
        document.querySelector("[data-ob-shell]").classList.toggle("onboarding-results", result);
        ["profile-header", "profile-progress", "profile-footer"].forEach(part => {
            document.querySelector(`[data-ob-${part}]`).hidden = result;
        });
        document.querySelector("[data-ob-result-header]").hidden = !result;
        document.querySelector("[data-ob-page-title]").textContent = step === 2 ? "Unggah Sertifikat" : "Profil Beasiswa";
        document.querySelector("[data-ob-step-label]").textContent = `LANGKAH ${Math.min(step, 2)} DARI 2`;
        document.querySelector("[data-ob-step-name]").textContent = step === 1 ? "Esai Diri" : "Sertifikat";
        const fill = document.querySelector("[data-ob-progress-bar]");
        fill.style.width = step === 1 ? "50%" : "100%";
        fill.parentElement.setAttribute("aria-valuenow", step === 1 ? "50" : "100");
        closeDropdown();
        if (!result) form.querySelector(`[data-ob-step="${step}"] h1`)?.focus({ preventScroll: true });
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    // =========================================================
    // Step 1: essay + chips
    // =========================================================
    const counter = document.querySelector("[data-ob-essay-count]");
    essay.addEventListener("input", () => {
        counter.textContent = essay.value.length;
        essay.classList.remove("is-invalid");
        errorEl("Essay").textContent = "";
    });

    const chipField = document.querySelector("[data-ob-chips]");
    const chipBox = chipField.querySelector("[data-ob-chips-box]");
    const chipOptions = (() => {
        try { return JSON.parse(chipField.dataset.options || "[]").map(o => ({ value: String(o.id), label: o.name })); }
        catch { return []; }
    })();

    const chips = []; // { value, label }
    let dd = null, ddItems = [], ddActive = -1;

    const chipInput = document.createElement("input");
    chipInput.type = "text";
    chipInput.id = "obChipInput";
    chipInput.className = "list-chip-input";
    chipInput.autocomplete = "off";
    chipInput.setAttribute("role", "combobox");
    chipInput.setAttribute("aria-expanded", "false");
    chipBox.appendChild(chipInput);

    function renderChips() {
        chipBox.querySelectorAll(".list-chip").forEach(c => c.remove());

        chips.forEach(chip => {
            const el = document.createElement("span");
            el.className = "list-chip";

            const text = document.createElement("span");
            text.className = "list-chip-text";
            text.textContent = chip.label;

            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "list-chip-remove";
            remove.setAttribute("aria-label", "Hapus");
            remove.addEventListener("click", () => removeChip(chip.value));

            if (typeof lucide !== "undefined") {
                remove.innerHTML = `<i data-lucide="x"></i>`;
            } else {
                remove.textContent = "\u00d7";
            }

            el.append(text, remove);
            chipInput.before(el);
            icons(el);
        });

        chipInput.placeholder = chips.length ? "" : (chipField.dataset.placeholder || "");
        document.querySelector("[data-ob-category-count]").textContent = chips.length;
    }

    function addChip(option) {
        if (chips.some(c => c.value === option.value)) return;
        chips.push(option);
        chipBox.classList.remove("is-invalid");
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
        const height = Math.min(dd.scrollHeight, 220) + 8;
        const below = window.innerHeight - rect.bottom;

        dd.style.position = "fixed";
        dd.style.left = `${rect.left}px`;
        dd.style.width = `${rect.width}px`;

        if (below < height && rect.top > below) {
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
            dd = document.createElement("div");
            dd.className = "list-chip-dd onboarding-chip-dropdown";
            dd.setAttribute("role", "listbox");
            dd.style.zIndex = "1080";
            // mousedown dicegah supaya input tidak kehilangan fokus
            dd.addEventListener("mousedown", e => e.preventDefault());
            document.body.appendChild(dd);
            chipInput.setAttribute("aria-expanded", "true");
        }

        dd.replaceChildren();

        if (!ddItems.length) {
            const empty = document.createElement("div");
            empty.className = "list-chip-dd-empty";
            empty.textContent = q ? "Tidak ada pilihan yang cocok" : "Tidak ada pilihan";
            dd.appendChild(empty);
        }

        ddItems.forEach((item, i) => {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "list-chip-dd-item" + (i === ddActive ? " active" : "");
            btn.setAttribute("role", "option");
            btn.textContent = item.label;
            btn.addEventListener("click", () => selectItem(i));
            dd.appendChild(btn);
        });

        positionDropdown();
        dd.querySelector(".active")?.scrollIntoView({ block: "nearest" });
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
        dd.querySelectorAll(".list-chip-dd-item").forEach((el, i) => el.classList.toggle("active", i === ddActive));
        dd.querySelector(".active")?.scrollIntoView({ block: "nearest" });
    }

    chipBox.addEventListener("click", e => {
        if (e.target.closest(".list-chip")) return;
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

    renderChips();

    function validateStep1() {
        clearErrors();
        let ok = true;
        const text = essay.value.trim();

        if (!text) {
            setError("Essay", "Essay Bebas wajib diisi.", essay);
            ok = false;
        } else if (text.length < 20 || text.length > 500) {
            setError("Essay", "Essay Bebas harus terdiri dari 20 sampai 500 karakter.", essay);
            ok = false;
        }

        if (!chips.length) {
            setError("CategoryIds", "Pilih minimal satu kategori.", chipBox);
            ok = false;
        }

        if (!ok) (form.querySelector(".is-invalid") === chipBox ? chipInput : essay).focus();
        return ok;
    }

    form.querySelector("[data-ob-next]").addEventListener("click", () => {
        if (validateStep1()) goTo(2);
    });
    form.querySelector("[data-ob-back]").addEventListener("click", () => goTo(1));
    document.querySelector("[data-ob-header-back]").addEventListener("click", e => {
        if (!form.querySelector('[data-ob-step="2"]').hidden) {
            e.preventDefault();
            goTo(1);
        }
    });

    // =========================================================
    // Step 2: file sertifikat (1 file; siap multi lewat MAX_FILES)
    // =========================================================
    const fileInput = document.getElementById("certificateInput");
    const fileList = form.querySelector("[data-ob-file-list]");
    const files = [];

    const sameFile = (a, b) => a.name === b.name && a.size === b.size && a.lastModified === b.lastModified;

    function selectFiles(selectedFiles) {
        errorEl("Certificates").textContent = "";
        fileInput.classList.remove("is-invalid");
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
        if (rejected.length) setError("Certificates", [...new Set(rejected)].join(" "), fileInput);
        renderFiles();
    }

    fileInput.addEventListener("change", () => selectFiles(fileInput.files));
    const dropzone = form.querySelector("[data-ob-dropzone]");
    ["dragenter", "dragover"].forEach(event => dropzone.addEventListener(event, e => {
        e.preventDefault();
        dropzone.classList.add("is-dragging");
    }));
    dropzone.addEventListener("dragleave", e => {
        if (!dropzone.contains(e.relatedTarget)) dropzone.classList.remove("is-dragging");
    });
    dropzone.addEventListener("drop", e => {
        e.preventDefault();
        dropzone.classList.remove("is-dragging");
        if (!submitting && e.dataTransfer?.files) selectFiles(e.dataTransfer.files);
    });

    function renderFiles() {
        fileList.querySelectorAll("img").forEach(img => URL.revokeObjectURL(img.src));
        fileList.replaceChildren();
        form.querySelector("[data-ob-file-count]").textContent = files.length;

        files.forEach((f, i) => {
            const li = document.createElement("li");
            li.className = "onboarding-file d-flex align-items-center gap-3";

            const img = document.createElement("img");
            img.src = URL.createObjectURL(f);
            img.alt = f.name;
            img.className = "onboarding-file-preview flex-shrink-0";

            const name = document.createElement("span");
            name.className = "d-block text-truncate fw-semibold";
            name.textContent = f.name;

            const info = document.createElement("div");
            info.className = "onboarding-file-info flex-grow-1";
            const status = document.createElement("span");
            status.className = "d-block small text-body-secondary mt-1";
            status.textContent = `${(f.size / 1024 / 1024).toLocaleString("id-ID", { maximumFractionDigits: 2 })} MB • Siap dianalisis`;
            info.append(name, status);

            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "btn onboarding-file-remove p-2 flex-shrink-0";
            remove.setAttribute("aria-label", "Hapus");
            remove.innerHTML = '<svg class="ob-icon" aria-hidden="true"><use href="#ob-trash" /></svg>';
            remove.setAttribute("aria-label", `Hapus ${f.name}`);
            remove.addEventListener("click", () => { files.splice(i, 1); renderFiles(); });

            li.append(img, info, remove);
            fileList.appendChild(li);
            icons(li);
        });
    }

    // =========================================================
    // Submit
    // =========================================================
    let submitting = false;

    form.addEventListener("submit", async e => {
        e.preventDefault();
        if (submitting) return;

        if (!validateStep1()) { goTo(1); return; }

        clearErrors();
        if (!files.length) {
            setError("Certificates", "Unggah minimal satu sertifikat.", fileInput);
            return;
        }

        const data = new FormData(form); // Essay + antiforgery token
        chips.forEach((c, i) => data.append(`CategoryIds[${i}]`, c.value));
        files.forEach(f => data.append("Certificates", f)); // key sama cocok untuk IFormFile maupun List<IFormFile>

        const button = form.querySelector("[data-ob-submit]");
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

            if (res.ok) {
                showResult(await res.json().catch(() => null));
                goTo(3);
                return;
            }

            if (res.status === 401 || res.status === 403) {
                window.location.assign("/identity/login?ReturnUrl=" + encodeURIComponent(location.pathname + location.search));
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

    const make = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text != null) node.textContent = text;
        return node;
    };

    function showResult(payload) {
        const box = form.querySelector("[data-ob-result]");
        box.replaceChildren();

        let recs = [];
        try { recs = JSON.parse(payload?.result || "{}").recommendations ?? []; }
        catch { /* output AI bukan JSON valid */ }

        if (!recs.length) {
            box.appendChild(make("div", "alert alert-secondary mb-0",
                "Belum ada beasiswa yang cocok dengan pilihanmu. Coba pilih kategori lain."));
            return;
        }

        const scholarships = new Map((payload?.scholarships ?? []).map(s => [s.id, s]));
        recs.forEach(r => box.appendChild(recommendationCard(r, scholarships.get(r.opportunityId))));
    }

    function recommendationCard(r, scholarship) {
        const score = Math.max(0, Math.min(100, Number(r.matchScore) || 0));
        const tone = score >= 80 ? "success" : score >= 60 ? "primary" : "warning";

        const card = make("article", "card border-0 shadow-sm ob-recommendation");
        const body = make("div", "card-body p-4");

        const head = make("div", "d-flex justify-content-between align-items-start gap-3 mb-3");
        head.append(
            make("h3", "h5 mb-0", scholarship?.name || r.opportunityName || "Beasiswa"),
            make("span", `badge text-bg-${tone} fs-6`, `${score}%`));

        const bar = make("div", "progress mb-3");
        bar.style.height = "6px";
        bar.setAttribute("role", "progressbar");
        bar.setAttribute("aria-valuenow", score);
        bar.setAttribute("aria-valuemin", 0);
        bar.setAttribute("aria-valuemax", 100);
        const fill = make("div", `progress-bar bg-${tone}`);
        fill.style.width = `${score}%`;
        bar.appendChild(fill);

        const reason = make("p", "mb-3", r.matchReason);

        const feedback = make("div", "alert alert-light border small mb-3");
        feedback.append(make("strong", null, "Saran: "), document.createTextNode(r.feedback ?? ""));

        const basis = make("details", "small text-muted");
        basis.append(make("summary", null, "Dasar perhitungan skor"), make("p", "mb-0 mt-2", r.matchScoreSource));

        body.append(head);
        if (scholarship) {
            const overview = make("div", "d-flex gap-3 align-items-start mb-3");
            const imageUrl = safeUrl(scholarship.contentImageUrl);
            if (imageUrl) {
                const image = make("img", "ob-recommendation-image rounded-3 flex-shrink-0");
                image.src = imageUrl;
                image.alt = `Poster ${scholarship.name}`;
                image.loading = "lazy";
                overview.append(image);
            }
            const info = make("div", "flex-grow-1");
            info.append(categoryBadges(scholarship.categories));
            const description = make("p", "ob-recommendation-description text-body-secondary small mb-2", scholarship.description);
            info.append(description, make("p", "small fw-semibold mb-1", prizeText(scholarship.totalPrize)),
                make("p", "small text-body-secondary mb-0", `Pendaftaran: ${dateText(scholarship.registrationStartAt)} – ${dateText(scholarship.registrationEndAt)}`));
            overview.append(info);
            body.append(overview);
        }
        body.append(make("div", "small fw-semibold text-body-secondary mb-2", "Kecocokan dengan profilmu"), bar, reason, feedback, basis);
        if (scholarship) {
            const actions = make("div", "d-flex flex-wrap justify-content-end gap-2 border-top pt-3 mt-3");
            const detail = make("button", "btn btn-outline-primary", "Lihat detail beasiswa");
            detail.type = "button";
            detail.setAttribute("data-bs-toggle", "modal");
            detail.setAttribute("data-bs-target", "#obScholarshipModal");
            detail.addEventListener("click", () => populateScholarshipModal(scholarship));
            actions.append(detail);
            const registration = externalLink(scholarship.registrationUrl, "Daftar beasiswa", "btn btn-primary");
            if (registration) actions.append(registration);
            body.append(actions);
        } else {
            body.append(make("p", "small text-body-secondary mt-3 mb-0", "Detail beasiswa ini belum tersedia."));
        }
        card.appendChild(body);
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

    function categoryBadges(categories) {
        const badges = make("div", "d-flex flex-wrap gap-2 mb-2");
        (categories ?? []).forEach(name => badges.append(make("span", "badge rounded-pill text-bg-light border", name)));
        return badges;
    }

    function populateScholarshipModal(s) {
        const modal = document.getElementById("obScholarshipModal");
        modal.querySelector(".modal-title").textContent = s.name;
        const body = modal.querySelector("[data-ob-modal-body]");
        body.replaceChildren();
        const row = make("div", "row g-4");
        const main = make("div", "col-md-8");
        const aside = make("div", "col-md-4");
        const imageUrl = safeUrl(s.contentImageUrl);
        if (imageUrl) {
            const imageLink = externalLink(imageUrl, null, "d-block mb-3");
            const image = make("img", "img-fluid rounded-3 w-100");
            image.src = imageUrl;
            image.alt = `Poster ${s.name}`;
            imageLink.append(image);
            aside.append(imageLink);
        }
        main.append(categoryBadges(s.categories));
        const description = make("section", "mb-4");
        description.append(make("h3", "h6 fw-bold", "Tentang beasiswa"), make("p", "ob-detail-text text-body-secondary mb-0", s.description || "Belum diinformasikan."));
        main.append(description);
        [["Persyaratan", s.requirement], ["Kontak", s.contactPerson]].forEach(([title, items]) => {
            const section = make("section", "mb-4");
            section.append(make("h3", "h6 fw-bold", title));
            if (items?.length) {
                const list = make("ul", "ps-3 mb-0");
                items.forEach(item => list.append(make("li", "ob-detail-text mb-2", item)));
                section.append(list);
            } else section.append(make("p", "text-body-secondary", "Belum diinformasikan."));
            main.append(section);
        });
        const schedule = make("div", "card bg-light border-0");
        const facts = make("dl", "card-body mb-0");
        [["Bantuan / total hadiah", prizeText(s.totalPrize)],
            ["Pendaftaran dibuka", dateText(s.registrationStartAt)],
            ["Pendaftaran ditutup", dateText(s.registrationEndAt)]].forEach(([label, value]) => {
            facts.append(make("dt", "small text-body-secondary fw-normal mb-1", label), make("dd", "fw-semibold mb-3", value));
        });
        schedule.append(facts);
        aside.append(schedule);
        row.append(main, aside);
        body.append(row);
        [["guide", s.guideUrl], ["register", s.registrationUrl]].forEach(([key, value]) => {
            const link = modal.querySelector(`[data-ob-modal-${key}]`);
            const url = safeUrl(value);
            link.hidden = !url;
            if (url) link.href = url;
            else link.removeAttribute("href");
        });
    }

    function showServerErrors(errors) {
        const invalidEl = { Essay: essay, CategoryIds: chipBox, Certificates: fileInput };
        let firstStep = 2;

        Object.entries(errors || {}).forEach(([key, messages]) => {
            const base = key.replace(/\[\d+\]/g, "");
            const known = base in invalidEl;
            if (base === "Essay" || base === "CategoryIds") firstStep = 1;
            setError(known ? base : "general", [...new Set(messages)].join(" "), invalidEl[base]);
        });

        goTo(firstStep);
    }
})();