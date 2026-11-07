(() => {
    // Script ini dieksekusi ulang tiap swap htmx. Listener di document
    // cukup dipasang sekali, kalau tidak submit terkirim berkali-kali.
    if (window.__formDrawerReady) return;
    window.__formDrawerReady = true;

    const esc = v => CSS.escape(v);
    const baseOf = key => key.replace(/\[\d+\]/g, ""); // Requirement[0] -> Requirement
    const icons = root => { if (typeof lucide !== "undefined") lucide.createIcons({ root }); };

    const toastError = msg => {
        try {
            if (typeof notyf !== "undefined" && notyf?.error) { notyf.error(msg); return; }
            if (typeof Notyf !== "undefined") {
                (window.__notyf ??= new Notyf({ position: { x: "right", y: "top" } })).error(msg);
                return;
            }
        } catch (err) {
            console.error("[form-drawer] toast gagal", err);
        }
        console.warn("[form-drawer] Notyf tidak ditemukan:", msg);
    };

    // input tampilan (angka berformat) -> hidden input yang dikirim ke server
    const targetOf = el => el.dataset?.moneyTarget ?? null;

    // =========================================================
    // Helper field list (dipakai validasi & submit, jadi ditaruh di atas)
    // =========================================================
    const isChips = field => field.dataset.listMode !== "rows"; // default: chips
    const rowsOf = field => [...field.querySelectorAll("[data-list-row]")];
    const chipsOf = field => [...field.querySelectorAll("[data-chip]")];
    const chipBox = field => field.querySelector("[data-list-items]");
    const chipInput = field => field.querySelector("[data-chip-input]");

    // nama field model untuk elemen apa pun (input biasa atau input tampilan)
    function fieldName(form, el) {
        if (el.name) return el.name;
        const target = targetOf(el);
        return target ? form.querySelector(target)?.name ?? null : null;
    }

    // elemen yang diberi tanda merah untuk sebuah nama field
    function inputFor(form, key) {
        // Requirement[2] -> item ke-3 yang benar-benar terkirim (baris/chip kosong dilewati)
        const item = key.match(/^(.*)\[(\d+)\]$/);
        const sent = item && form.__listSent?.[item[1]];
        if (sent) return sent[Number(item[2])] ?? null;

        // error level field list (mis. "kurang dari minimal") -> tandai kotak chips
        const listField = form.querySelector(`[data-list-field][data-name="${esc(key)}"]`);
        if (listField && isChips(listField)) return chipBox(listField);

        const input = form.querySelector(`[name="${esc(key)}"]`);
        if (input?.type === "hidden" && input.id) {
            return form.querySelector(`[data-money-target="#${esc(input.id)}"]`) ?? input;
        }
        return input;
    }

    const summaryOf = form => form.querySelector("[data-valmsg-summary]");
    const spanFor = (form, key) =>
        key ? form.querySelector(`[data-valmsg-for="${esc(baseOf(key))}"]`) : null;

    function clearErrors(form) {
        form.querySelectorAll("[data-valmsg-for]").forEach(s => (s.textContent = ""));
        form.querySelectorAll(".is-invalid").forEach(i => i.classList.remove("is-invalid"));
        const summary = summaryOf(form);
        if (summary) summary.textContent = "";
    }

    // fokus ke elemen merah pertama (untuk chips: fokus ke input ketiknya)
    function focusFirstInvalid(form) {
        const el = form.querySelector(".is-invalid");
        if (!el) return;

        if (el.matches("[data-list-items]") || el.matches("[data-chip]")) {
            el.closest("[data-list-field]")?.querySelector("[data-chip-input]")?.focus();
            return;
        }
        el.focus();
    }

    function showErrors(form, errors) {
        const general = [];
        const bySpan = new Map();

        Object.entries(errors || {}).forEach(([key, messages]) => {
            const span = spanFor(form, key);

            if (!span) {
                general.push(...messages);
                return;
            }

            bySpan.set(span, [...(bySpan.get(span) ?? []), ...messages]);
            inputFor(form, key)?.classList.add("is-invalid");
        });

        bySpan.forEach((messages, span) => {
            span.textContent = [...new Set(messages)].join(" ");
        });

        const summary = summaryOf(form);
        if (summary && general.length) {
            summary.textContent = general.join(" ");
            summary.scrollIntoView({ block: "nearest", behavior: "smooth" });
        }

        if (general.length) toastError(general.join(" "));

        focusFirstInvalid(form);
    }

    // =========================================================
    // Field list -- mode chips (default) & rows
    // =========================================================

    // tombol "Tambah ..." (hanya mode rows)
    function ensureAddButton(field) {
        const items = field.querySelector("[data-list-items]");
        if (!items || field.querySelector("[data-list-add]")) return;

        const add = document.createElement("button");
        add.type = "button";
        add.className = "btn btn-outline-primary btn-sm mt-2 text-xs";
        add.dataset.listAdd = "";
        add.innerHTML = `<i style="width: 16px; height: 16px; margin-top: -3.25px;" data-lucide="plus"></i> ${field.dataset.addLabel || "Tambah"}`;
        items.after(add);
        icons(add);
    }

    // ---------------------------------------------------------
    // Entri chip = { value, label }
    //   value = yang dikirim ke server (id / teks)
    //   label = yang tampil di chip & dropdown
    // ---------------------------------------------------------
    const lower = s => String(s).toLowerCase();

    // string | angka | objek ({Id, Name} / {Value, Text} / ...) -> { value, label } | null
    function entryOf(o) {
        if (typeof o === "number") o = String(o);

        if (typeof o === "string") {
            const t = o.trim();
            return t ? { value: t, label: t } : null;
        }

        if (!o || typeof o !== "object") return null;

        const read = keys => {
            for (const k of keys) {
                const key = Object.keys(o).find(x => x.toLowerCase() === k);
                if (key && o[key] != null && String(o[key]).trim()) return String(o[key]).trim();
            }
            return "";
        };

        const label = read(["name", "label", "text", "title"]) || read(["value", "id"]);
        const value = read(["id", "value"]) || label;
        return label ? { value, label } : null;
    }

    // data-options -> [{ value, label }], tanpa duplikat
    function optionsOf(field) {
        if (field.__options) return field.__options;

        let list = [];
        try {
            const parsed = JSON.parse(field.dataset.options || "[]");
            if (Array.isArray(parsed)) list = parsed.map(entryOf).filter(Boolean);
        } catch { }

        const seen = new Set();
        field.__options = list.filter(o => !seen.has(lower(o.value)) && seen.add(lower(o.value)));
        return field.__options;
    }

    const allowCustom = field => field.dataset.listAllowCustom === "true";

    // teks yang diketik -> entri yang boleh jadi chip (null = tidak boleh)
    function resolveEntry(field, text) {
        const opts = optionsOf(field);
        if (!opts.length) return { value: text, label: text };

        const hit = opts.find(o => lower(o.label) === lower(text))
            ?? opts.find(o => lower(o.value) === lower(text));
        if (hit) return hit;

        return allowCustom(field) ? { value: text, label: text } : null;
    }

    const selectedSet = field => new Set(chipsOf(field).map(c => lower(c.dataset.value)));

    function buildItems(field, q) {
        const picked = selectedSet(field);
        const ql = lower(q);

        const items = optionsOf(field)
            .filter(o => !picked.has(lower(o.value)) && (!ql || lower(o.label).includes(ql)))
            .sort((a, b) => {
                const sa = ql && lower(a.label).startsWith(ql) ? 0 : 1;
                const sb = ql && lower(b.label).startsWith(ql) ? 0 : 1;
                return sa - sb;
            })
            .slice(0, 100)
            .map(o => ({ value: o.value, label: o.label, custom: false }));

        const exact = optionsOf(field).some(o => lower(o.label) === ql) || picked.has(ql);
        if (allowCustom(field) && q && !exact) items.push({ value: q, label: q, custom: true });

        return items;
    }

    function fillHighlighted(el, text, q) {
        const i = q ? lower(text).indexOf(lower(q)) : -1;
        if (i < 0) {
            el.textContent = text;
            return;
        }

        const mark = document.createElement("mark");
        mark.textContent = text.slice(i, i + q.length);
        el.append(
            document.createTextNode(text.slice(0, i)),
            mark,
            document.createTextNode(text.slice(i + q.length))
        );
    }

    function positionDropdown(field) {
        const dd = field.__dd;
        const box = chipBox(field);

        if (!dd || !box) return;

        const rect = box.getBoundingClientRect();
        const height = Math.min(dd.scrollHeight, 220) + 8;

        const below = window.innerHeight - rect.bottom;
        const above = rect.top;

        dd.style.left = `${rect.left}px`;
        dd.style.width = `${rect.width}px`;

        if (below < height && above > below) {
            dd.style.top = "auto";
            dd.style.bottom = `${window.innerHeight - rect.top + 2}px`;
        } else {
            dd.style.bottom = "auto";
            dd.style.top = `${rect.bottom + 2}px`;
        }

        dd.querySelector(".active")?.scrollIntoView({
            block: "nearest"
        });
    }

    function renderDropdown(field, resetActive = true) {
        const input = chipInput(field);
        if (!input || !optionsOf(field).length) return;

        const q = input.value.trim();
        const items = buildItems(field, q);
        field.__items = items;

        if (resetActive || !(field.__active >= 0 && field.__active < items.length)) {
            field.__active = items.length ? 0 : -1;
        }

        let dd = field.__dd;
        if (!dd) {
            dd = document.createElement("div");
            dd.className = "list-chip-dd";
            dd.dataset.chipDd = "";
            dd.setAttribute("role", "listbox");
            const drawer = field.closest(".offcanvas");

            (drawer ?? document.body).appendChild(dd);

            dd.__field = field;
            field.__dd = dd;
            input.setAttribute("aria-expanded", "true");
        }

        dd.replaceChildren();

        if (!items.length) {
            const empty = document.createElement("div");
            empty.className = "list-chip-dd-empty";
            empty.textContent = q ? "Tidak ada pilihan yang cocok" : "Tidak ada pilihan";
            dd.appendChild(empty);
        }

        items.forEach((item, i) => {
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "list-chip-dd-item" + (i === field.__active ? " active" : "");
            btn.dataset.chipDdItem = String(i);
            btn.setAttribute("role", "option");

            if (item.custom) {
                const strong = document.createElement("strong");
                strong.textContent = `"${item.label}"`;
                btn.append(document.createTextNode("Tambah "), strong);
            } else {
                fillHighlighted(btn, item.label, q);
            }

            dd.appendChild(btn);
        });

        positionDropdown(field);
    }

    function closeDropdown(field) {
        if (!field.__dd) return;
        field.__dd.remove();
        field.__dd = null;
        field.__items = [];
        chipInput(field)?.setAttribute("aria-expanded", "false");
    }

    function moveActive(field, step) {
        const items = field.__items ?? [];
        if (!items.length) return;

        field.__active = (field.__active + step + items.length) % items.length;
        field.__dd?.querySelectorAll(".list-chip-dd-item").forEach((el, i) => {
            el.classList.toggle("active", i === field.__active);
        });
        field.__dd?.querySelector(".active")?.scrollIntoView({ block: "nearest" });
    }

    function selectItem(field, index) {
        const item = field.__items?.[index];
        const input = chipInput(field);
        if (!item || !input) return;

        input.value = "";
        addChip(field, item.value, item.label);
        closeDropdown(field);
        input.focus();
    }

    // ---------------------------------------------------------
    // Mode chips
    // ---------------------------------------------------------
    function clearFieldError(field) {
        chipBox(field)?.classList.remove("is-invalid");
        chipsOf(field).forEach(c => c.classList.remove("is-invalid"));
        const span = field.querySelector("[data-valmsg-for]");
        if (span) span.textContent = "";
    }

    function updatePlaceholder(field) {
        const input = chipInput(field);
        if (input) input.placeholder = chipsOf(field).length ? "" : (field.dataset.placeholder || "");
    }

    // value = yang dikirim ke server, label = yang tampil (default sama dengan value)
    function addChip(field, value, label = value) {
        const val = (value ?? "").trim();
        const text = (label ?? "").trim() || val;
        const input = chipInput(field);
        if (!val || !input) return null;

        // field dengan daftar pilihan: satu nilai cukup satu chip
        if (optionsOf(field).length && selectedSet(field).has(lower(val))) return null;

        const chip = document.createElement("span");
        chip.className = "list-chip";
        chip.dataset.chip = "";
        chip.dataset.value = val;

        const labelEl = document.createElement("span");
        labelEl.className = "list-chip-text";
        labelEl.textContent = text;

        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "list-chip-remove";
        remove.dataset.chipRemove = "";
        remove.setAttribute("aria-label", "Hapus");

        chip.append(labelEl, remove);
        input.before(chip);

        if (typeof lucide !== "undefined") {
            remove.innerHTML = `<i data-lucide="x"></i>`;
            icons(chip);
        } else {
            remove.textContent = "\u00d7";
        }

        clearFieldError(field);
        updatePlaceholder(field);
        return chip;
    }

    // teks yang masih diketik di input -> jadi chip (kalau valid; kalau tidak, teks dibiarkan)
    function commitPending(field) {
        const input = chipInput(field);
        if (!input) return;

        const text = input.value.trim();
        if (!text) {
            input.value = "";
            return;
        }

        const entry = resolveEntry(field, text);
        if (!entry) return;

        input.value = "";
        addChip(field, entry.value, entry.label);
    }

    function setupChips(field) {
        const box = chipBox(field);
        if (!box || field.dataset.chipsReady === "1") return;
        field.dataset.chipsReady = "1";

        // nilai awal: baris yang dirender server, kalau tidak ada pakai data-values
        let values = [];
        try {
            const parsed = JSON.parse(field.dataset.values || "[]");
            if (Array.isArray(parsed)) values = parsed;
        } catch { }

        const fromRows = rowsOf(field).map(row => row.querySelector("input")?.value ?? "");
        if (fromRows.length) values = fromRows;

        box.className = "list-chip-box form-control";
        box.replaceChildren();

        const input = document.createElement("input");
        input.type = "text";
        input.className = "list-chip-input";
        input.autocomplete = "off";
        input.dataset.chipInput = "";

        const opts = optionsOf(field);
        if (opts.length) {
            input.setAttribute("role", "combobox");
            input.setAttribute("aria-autocomplete", "list");
            input.setAttribute("aria-expanded", "false");
        }

        // klik label ikut memfokuskan input chips
        const label = field.querySelector("label");
        if (label?.htmlFor) input.id = label.htmlFor;
        else if (label) input.setAttribute("aria-label", label.textContent.trim());

        box.appendChild(input);

        // nilai awal -> chip. Dengan daftar pilihan, id dicocokkan ke label-nya.
        values.map(entryOf).filter(Boolean).forEach(e => {
            if (!opts.length) {
                addChip(field, e.value, e.label);
                return;
            }

            const hit = opts.find(o => lower(o.value) === lower(e.value))
                ?? opts.find(o => lower(o.label) === lower(e.label));

            if (hit) addChip(field, hit.value, hit.label);
            else if (allowCustom(field)) addChip(field, e.value, e.label);
        });

        updatePlaceholder(field);
    }

    // ---------------------------------------------------------
    // Mode rows (tampilan lama)
    // ---------------------------------------------------------

    // nama input di DOM: Name[0], Name[1], ... (urut sesuai baris)
    function reindex(field) {
        rowsOf(field).forEach((row, i) => {
            const input = row.querySelector("input");
            if (input) input.name = `${field.dataset.name}[${i}]`;
        });
    }

    function addRow(field, value = "", focus = false) {
        const row = document.createElement("div");
        row.className = "input-group";
        row.dataset.listRow = "";
        row.innerHTML = `
            <input type="text" class="form-control" autocomplete="off" />
            <button type="button" class="btn btn-outline-danger" data-list-remove aria-label="Hapus">
                <i data-lucide="x"></i>
            </button>`;

        const input = row.querySelector("input");
        input.placeholder = field.dataset.placeholder || "";
        input.value = value;

        field.querySelector("[data-list-items]").appendChild(row);
        reindex(field);
        icons(row);
        if (focus) input.focus();
        return row;
    }

    function initList(field) {
        const items = field.querySelector("[data-list-items]");
        if (!items) return;

        if (isChips(field)) {
            setupChips(field);
            return;
        }

        ensureAddButton(field);

        if (rowsOf(field).length === 0) {
            let values = [];
            try {
                const parsed = JSON.parse(field.dataset.values || "[]");
                if (Array.isArray(parsed)) values = parsed;
            } catch { }

            values = values.filter(v => typeof v === "string" && v.trim());
            if (values.length) values.forEach(v => addRow(field, v));
            else addRow(field);
        }

        else reindex(field);
    }

    const initLists = root => root.querySelectorAll("[data-list-field]").forEach(initList);

    // dropdown: mousedown dicegah supaya input tidak kehilangan fokus (kalau tidak, teks ketikan ikut ter-commit)
    document.addEventListener("mousedown", e => {
        if (e.target.closest?.("[data-chip-dd]")) e.preventDefault();
    });

    document.addEventListener("click", e => {
        const field =
            e.target.closest("[data-list-field]") ??
            e.target.closest("[data-chip-dd]")?.__field;

        if (!field) return;

        // ---- chips: pilih item dropdown ----
        const ddItem = e.target.closest("[data-chip-dd-item]");
        if (ddItem) {
            selectItem(field, Number(ddItem.dataset.chipDdItem));
            return;
        }

        // ---- chips: hapus chip ----
        const chipRemove = e.target.closest("[data-chip-remove]");
        if (chipRemove) {
            chipRemove.closest("[data-chip]")?.remove();
            updatePlaceholder(field);
            chipInput(field)?.focus();
            if (field.__dd) renderDropdown(field, false);
            return;
        }

        // ---- rows: tambah baris ----
        if (e.target.closest("[data-list-add]")) {
            if (!isChips(field)) addRow(field, "", true);
            return;
        }

        // klik area kosong di kotak chips = fokus ke input (+ buka dropdown kalau ada pilihan)
        if (isChips(field) && e.target.closest("[data-list-items]") && !e.target.closest("[data-chip]")) {
            chipInput(field)?.focus();
            if (optionsOf(field).length && !field.__dd) renderDropdown(field);
            return;
        }

        // ---- rows: hapus baris ----
        const remove = e.target.closest("[data-list-remove]");
        if (remove) {
            const rows = rowsOf(field);
            const row = remove.closest("[data-list-row]");

            // selalu sisakan 1 baris: baris terakhir cukup dikosongkan
            if (rows.length <= 1) {
                const input = row.querySelector("input");
                input.value = "";
                input.classList.remove("is-invalid");
                input.focus();
            } else {
                row.remove();
                reindex(field);
            }
        }
    });

    // capture = true: Esc di dropdown harus menutup dropdown saja, bukan ikut menutup drawer
    document.addEventListener("keydown", e => {
        if (e.isComposing) return;

        const chipEl = e.target.closest?.("[data-chip-input]");
        if (chipEl) {
            const field = chipEl.closest("[data-list-field]");
            if (!field) return;

            const hasOptions = optionsOf(field).length > 0;

            if (e.key === "ArrowDown" || e.key === "ArrowUp") {
                if (!hasOptions) return;
                e.preventDefault();

                if (!field.__dd) renderDropdown(field);
                else moveActive(field, e.key === "ArrowDown" ? 1 : -1);
                return;
            }

            if (e.key === "Escape") {
                if (field.__dd) {
                    e.preventDefault();
                    e.stopPropagation();
                    closeDropdown(field);
                }
                return;
            }

            if (e.key === "Enter") {
                e.preventDefault();
                                    
                // item dropdown yang sedang aktif menang atas teks mentah
                if (field.__dd && field.__items?.[field.__active]) selectItem(field, field.__active);
                else commitPending(field);
                return;
            }

            if (e.key === "Backspace" && !chipEl.value) {
            const last = chipsOf(field).pop();
        if (last) {
                last.remove();
                updatePlaceholder(field);
                if (field.__dd) renderDropdown(field, false);
        }
                }
            return;
        }

            // rows: Enter di input list = baris berikutnya (bukan submit)
            if (e.key !== "Enter") return;
            const row = e.target.closest?.("[data-list-row]");
            const field = row?.closest("[data-list-field]");
            if (!field || e.target.tagName !== "INPUT") return;

            e.preventDefault();
            if (!e.target.value.trim()) return;

            const rows = rowsOf(field);
            const next = rows[rows.indexOf(row) + 1];
            if (next) next.querySelector("input").focus();
            else addRow(field, "", true);
        }, true);

    // chips: paste teks multi-baris = satu chip per baris (yang tidak valid dilewati)
    document.addEventListener("paste", e => {
        const input = e.target.closest?.("[data-chip-input]");
        const field = input?.closest("[data-list-field]");
        if (!field) return;

        const text = e.clipboardData?.getData("text") ?? "";
        if (!/[\r\n]/.test(text)) return;

        e.preventDefault();
        text.split(/\r?\n/).forEach(line => {
            const t = line.trim();
            const entry = t ? resolveEntry(field, t) : null;
            if (entry) addChip(field, entry.value, entry.label);
        });
        if (field.__dd) renderDropdown(field, false);
    });

    // chips: fokus ke input = buka dropdown (kalau ada pilihan)
    document.addEventListener("focusin", e => {
        const input = e.target.closest?.("[data-chip-input]");
        const field = input?.closest("[data-list-field]");
        if (field && optionsOf(field).length) renderDropdown(field);
    });

    // chips: input kehilangan fokus = tutup dropdown, teks valid yang tertinggal jadi chip
    document.addEventListener("focusout", e => {
        const input = e.target.closest?.("[data-chip-input]");
        const field = input?.closest("[data-list-field]");
        if (!field) return;

        commitPending(field);
        closeDropdown(field);
    });

    // kumpulkan nilai list: buang yang kosong, indeks dirapikan, validasi minimal
    // yang dikirim = value chip (id / teks), bukan label
    function collectLists(form, data) {
        const sent = {};
        const errors = {};

        form.querySelectorAll("[data-list-field]").forEach(field => {
            const name = field.dataset.name;
            if (!name) return;

            // hapus semua Name[i] bawaan DOM, ganti dengan versi bersih
            [...data.keys()]
    .filter(k => k.startsWith(`${name}[`))
    .forEach(k => data.delete(k));

            let items, valueOf;
            let leftover = "";

            if (isChips(field)) {
    setupChips(field); // jaga-jaga kalau belum sempat diinisialisasi
    commitPending(field); // teks yang belum di-Enter ikut terkirim
    closeDropdown(field);
    items = chipsOf(field);
    valueOf = chip => chip.dataset.value;
    leftover = chipInput(field)?.value.trim() ?? ""; // teks yang tidak ada di daftar pilihan
            } else {
    items = rowsOf(field)
        .map(row => row.querySelector("input"))
        .filter(input => input && input.value.trim());
    valueOf = input => input.value.trim();
            }

            items.forEach((item, i) => data.append(`${name}[${i}]`, valueOf(item)));
            sent[name] = items;

            const min = Number(field.dataset.listMin ?? 1);
            if (leftover) {
    errors[name] = [`"${leftover}" tidak ada di daftar pilihan.`];
            } else if (items.length < min) {
    const label = field.querySelector("label")?.textContent.trim().toLowerCase() || "item";
    errors[name] = [field.dataset.listMessage || `Minimal ${min === 1 ? "satu" : min} ${label} harus ditambahkan.`];
            }
        });

        form.__listSent = sent;
        return errors;
    }

    // ---- saat mengetik: format angka + hapus error field itu ----
    document.addEventListener("input", e => {
        const el = e.target;

        // chips: mengetik menghapus error field list itu + menyaring dropdown
        const chipEl = el.closest?.("[data-chip-input]");
        if (chipEl) {
            const field = chipEl.closest("[data-list-field]");
            if (field) {
                clearFieldError(field);
                if (optionsOf(field).length) renderDropdown(field);
            }
            return;
        }

        const form = el.closest?.("form[data-ajax-form]");
        if (!form) return;

        // format angka: tampil 100.000, nilai asli 100000
        const target = targetOf(el);
        if (target) {
            const digits = el.value.replace(/\D/g, "").replace(/^0+(?=\d)/, "");
            const before = el.value.slice(0, el.selectionStart).replace(/\D/g, "").length;

            el.value = digits ? Number(digits).toLocaleString("id-ID") : "";

            // kursor tetap di posisi angka yang sama setelah titik ditambahkan
            let seen = 0, pos = 0;
            while (pos < el.value.length && seen < before) {
                if (/\d/.test(el.value[pos])) seen++;
                pos++;
            }

            el.setSelectionRange(pos, pos);

            const hidden = form.querySelector(target);
            if (hidden) hidden.value = digits;
        }

        const name = fieldName(form, el);
        if (!name) return;

        el.classList.remove("is-invalid");

        // untuk list (rows): error span hilang kalau tidak ada lagi input merah di field itu
        const span = spanFor(form, name);
        if (!span) return;

        const field = el.closest("[data-list-field]");
        if (!field || !field.querySelector(".is-invalid")) span.textContent = "";
    });

    // ---- submit lewat fetch ----
    document.addEventListener("submit", async e => {
        const form = e.target.closest("form[data-ajax-form]");
        if (!form) return;

        e.preventDefault();

        // pengaman klik ganda / event ganda
        if (form.dataset.submitting === "1") return;

        clearErrors(form);

        const data = new FormData(form); // sudah termasuk antiforgery token + file
        const clientErrors = collectLists(form, data);
        if (Object.keys(clientErrors).length) {
            showErrors(form, clientErrors);
            return;
        }

        form.dataset.submitting = "1";

        const button = form.querySelector('[type="submit"]');
        let navigating = false;
        if (button) button.disabled = true;

        try {
            const res = await fetch(form.action, {
                method: "POST",
                body: data,
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (res.ok) {
                const json = await res.json();
                navigating = true;
                window.location.assign(json.redirect || window.location.pathname);
                return;
            }

            if (res.status === 401 || res.status === 403) {
                navigating = true;
                window.location.assign("/identity/login?ReturnUrl=" + encodeURIComponent(location.pathname + location.search));
                return;
            }

            if (res.status === 422) {
                showErrors(form, (await res.json()).errors);
                return;
            }

            throw new Error(`HTTP ${res.status}`);
        } catch (err) {
            console.error("[form-drawer]", err);
            showErrors(form, { "": ["Terjadi kesalahan. Silakan coba lagi."] });
        } finally {
            if (!navigating) {
                        delete form.dataset.submitting;
                        if (button) button.disabled = false;
                    }
                }
            });

    // ---- drawer ditutup: bersihkan error + tutup dropdown ----
    document.addEventListener("hidden.bs.offcanvas", e => {
        const form = e.target.querySelector?.("form[data-ajax-form]");
        if (form) clearErrors(form);
        e.target.querySelectorAll?.("[data-list-field]").forEach(closeDropdown);
    });

    // ---- drawer dibuka: siapkan field list + ikon ----
    document.addEventListener("show.bs.offcanvas", e => {
        initLists(e.target);
        icons(e.target);
    });

    // drawer yang sudah terlanjur terbuka (OpenOnLoad) sebelum listener ini terpasang
    document.querySelectorAll(".offcanvas.show, .offcanvas.showing").forEach(el => {
        initLists(el);
        icons(el);
    });
})();