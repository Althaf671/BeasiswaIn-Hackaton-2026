/*!
 * data-grid.js
 * Grid kartu reusable dengan toolbar yang sama dengan data-table.js: search, chip filter, tombol, paginasi.
 * Dipakai lewat partial _DataGrid.cshtml, atau manual: AppDataGrid.init(element, options).
 *
 * Event: "datagrid:action" { action, id, value, row } untuk aksi tanpa href dan tombol toolbar.
 */
(() => {
  "use strict";

  const LOCALE = "id-ID";

  const DEFAULTS = {
    idKey: "id",
    pageSize: 12,
    searchPlaceholder: "Cari...",
    emptyText: "Belum ada data",
    zeroText: "Data tidak ditemukan",
    filters: true,       // true = otomatis (status/boolean/tanggal) | false | ["categoryName", "createdAt"]
    options: {},         // { categoryName: ["Pernikahan", "Wisuda"] } -> opsi chip walau belum ada datanya
    labels: {},          // { categoryName: "Kategori" }
    badges: {},          // { isActive: { labels: {...}, variants: {...} } }
    card: {},            // { image, title, subtitle, badge, description, meta: ["price"] }
    aspect: "4 / 3",
    minCardWidth: 260,
    searchKeys: null,
    actions: null,       // sama dengan data-table: { name, label, icon, href: "/x/{id}", toggle, target, variant, items }
    detailUrl: null,
    toolbar: null,
    preview: true,       // klik gambar membuka lightbox
    defaultSort: { key: "createdAt", dir: "desc" }  // atau false
  };

  const DEFAULT_LABELS = {
    createdAt: "Dibuat", updatedAt: "Diperbarui", status: "Status", isActive: "Status",
    categoryName: "Kategori", price: "Harga", durationInMinutes: "Durasi (menit)",
    itemCount: "Jumlah Item", galleryCategoryName: "Kategori"
  };

  const DATE_PRESETS = [
    { label: "Hari ini", days: 0 },
    { label: "7 hari terakhir", days: 6 },
    { label: "30 hari terakhir", days: 29 },
    { label: "Bulan ini", month: true }
  ];

  const ISO_DATE = /^\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?$/;
  const STATUS_KEY = /status$|^state$/i;
  const CURRENCY_KEY = /price|harga|amount|fee|cost/i;

  const currencyFmt = new Intl.NumberFormat(LOCALE, { style: "currency", currency: "IDR", maximumFractionDigits: 0 });
  const numberFmt = new Intl.NumberFormat(LOCALE);

  // ---------- helpers ----------
  const pad = n => String(n).padStart(2, "0");
  const toKey = d => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  const isBlank = v => v === null || v === undefined || v === "";
  const optionKey = v => String(v ?? "").toLowerCase();
  const lowerKeys = obj => Object.fromEntries(Object.entries(obj ?? {}).map(([k, v]) => [optionKey(k), v]));
  const slug = v => String(v ?? "").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");
  const boolLabel = v => (v ? "Aktif" : "Nonaktif");

  const escapeHtml = value =>
    String(value ?? "").replace(/[&<>"']/g, c => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));

  const humanize = key => {
    const t = String(key).replace(/([a-z0-9])([A-Z])/g, "$1 $2").replace(/[_-]+/g, " ");
    return t.charAt(0).toUpperCase() + t.slice(1);
  };

  const formatDate = v => {
    const d = new Date(v);
    return Number.isNaN(d.getTime())
      ? "-"
      : d.toLocaleDateString(LOCALE, { day: "numeric", month: "short", year: "numeric" });
  };

  const formatKey = key =>
    new Date(key + "T00:00:00").toLocaleDateString(LOCALE, { day: "numeric", month: "short" });

  const toTime = v => {
    const t = isBlank(v) ? NaN : new Date(v).getTime();
    return Number.isNaN(t) ? 0 : t;
  };

  const fillTemplate = (tpl, row) =>
    String(tpl).replace(/\{(\w+)\}/g, (_, k) => encodeURIComponent(row?.[k] ?? ""));

  function readJson(root, selector, fallback) {
    const el = root.querySelector(selector);
    if (!el) return fallback;
    try { return JSON.parse(el.textContent || "") ?? fallback; }
    catch (err) { console.error(`[data-grid] JSON ${selector} tidak valid:`, err); return fallback; }
  }

  function unwrapRows(payload) {
    if (Array.isArray(payload)) return payload;
    if (payload && Array.isArray(payload.items)) return payload.items;
    if (payload && Array.isArray(payload.data)) return payload.data;
    return [];
  }

  const renderIcons = root => { if (typeof lucide !== "undefined") lucide.createIcons({ root }); };

  const formatValue = (key, v) => {
    if (isBlank(v)) return "-";
    if (typeof v === "boolean") return boolLabel(v);
    if (typeof v === "number") return CURRENCY_KEY.test(key) ? currencyFmt.format(v) : numberFmt.format(v);
    if (typeof v === "string" && ISO_DATE.test(v)) return formatDate(v);
    return String(v);
  };

  // ---------- card mapping ----------
  function resolveCard(rows, card) {
    const keys = Object.keys(rows.find(Boolean) ?? {});
    const find = re => keys.find(k => re.test(k));

    return {
      image: card.image ?? find(/image|photo|thumbnail/i) ?? null,
      title: card.title ?? find(/^(title|name)$/i) ?? keys[0] ?? "id",
      subtitle: card.subtitle ?? null,
      badge: card.badge ?? null,
      description: card.description ?? null,
      meta: Array.isArray(card.meta) ? card.meta : []
    };
  }

  // ---------- filters ----------
  function normalizeSchema(raw) {
    const map = new Map();
    (Array.isArray(raw) ? raw : []).forEach(item => {
      if (item?.key) map.set(item.key, { type: item.type ?? "text", options: item.options ?? [] });
    });
    return map;
  }

  function resolveFilters(rows, cfg, schema) {
    if (cfg.filters === false) return [];

    // otomatis: kolom enum, boolean, dan tanggal createdAt (urutan = urutan properti C#)
    const keys = Array.isArray(cfg.filters)
      ? cfg.filters
      : [...schema.entries()]
          .filter(([k, s]) => s.type === "enum" || s.type === "boolean" || (s.type === "date" && /^createdAt$/i.test(k)))
          .map(([k]) => k);

    return keys.map(key => {
      const hint = schema.get(key);
      const sample = rows.map(r => r?.[key]).find(v => !isBlank(v));
      const title = cfg.labels[key] ?? DEFAULT_LABELS[key] ?? humanize(key);

      const isDate = hint?.type === "date" || (typeof sample === "string" && ISO_DATE.test(sample));
      if (isDate) return { key, title, kind: "range" };

      const labels = lowerKeys(cfg.badges?.[key]?.labels);
      const map = new Map();
      const add = (v, label) => {
        if (isBlank(v)) return;
        const k = optionKey(v);
        if (!map.has(k)) {
          map.set(k, labels[k] ?? label ?? (typeof v === "boolean" ? boolLabel(v) : humanize(String(v))));
        }
      };

      if (hint?.type === "boolean" || typeof sample === "boolean") { add(true); add(false); }
      (hint?.options ?? []).forEach(o => add(o.value, humanize(o.label ?? o.value)));
      (cfg.options?.[key] ?? []).forEach(v => add(v, String(v)));
      Object.keys(labels).forEach(k => add(k));
      rows.forEach(r => add(r?.[key]));

      return { key, title, kind: "set", options: [...map].map(([value, label]) => ({ value, label })) };
    }).filter(f => f.kind === "range" || f.options.length);
  }
  const createFilterState = filters =>
    Object.fromEntries(filters.map(f => [f.key, f.kind === "set" ? new Set() : { from: "", to: "" }]));

  function presetRange(preset) {
    const today = new Date();
    const to = toKey(today);
    if (preset.month) return { from: toKey(new Date(today.getFullYear(), today.getMonth(), 1)), to };
    const from = new Date(today);
    from.setDate(from.getDate() - preset.days);
    return { from: toKey(from), to };
  }

  function chipValue(filter, state) {
    const cur = state[filter.key];

    if (filter.kind === "set") {
      const labels = filter.options.filter(o => cur.has(o.value)).map(o => o.label);
      if (!labels.length) return "";
      return labels.length > 1 ? `${labels[0]}, +${labels.length - 1}` : labels[0];
    }

    const { from, to } = cur;
    if (!from && !to) return "";
    if (from && to) return from === to ? formatKey(from) : `${formatKey(from)} – ${formatKey(to)}`;
    return from ? `Sejak ${formatKey(from)}` : `Hingga ${formatKey(to)}`;
  }

  const resetFilter = (f, state) => {
    if (f.kind === "set") state[f.key].clear();
    else state[f.key] = { from: "", to: "" };
  };

  function matchesFilters(filters, state, row) {
    for (const f of filters) {
      const cur = state[f.key];

      if (f.kind === "set") {
        if (cur.size && !cur.has(optionKey(row?.[f.key]))) return false;
        continue;
      }

      const { from, to } = cur;
      if (!from && !to) continue;
      if (isBlank(row?.[f.key])) return false;

      const k = toKey(new Date(row[f.key]));
      if (from && k < from) return false;
      if (to && k > to) return false;
    }
    return true;
  }

  // ---------- markup (class sama dengan data-table, jadi SCSS-nya dipakai bersama) ----------
  function chipHtml(filter, popoverHtml, popoverClass = "") {
    const label = escapeHtml(filter.title);
    return `
      <div class="filter-chip" data-filter="${escapeHtml(filter.key)}">
        <div class="filter-chip__main">
          <button type="button" class="btn filter-chip__trigger" aria-haspopup="true" aria-expanded="false">
            <span class="filter-chip__label">${label}</span>
            <span class="filter-chip__value" hidden></span>
            <i data-lucide="chevron-down" class="filter-chip__chevron"></i>
          </button>
          <button type="button" class="btn filter-chip__clear" aria-label="Hapus filter ${label}" hidden>
            <i data-lucide="x"></i>
          </button>
        </div>
        <div class="filter-popover ${popoverClass}" hidden>${popoverHtml}</div>
      </div>`;
  }

  const setPopoverHtml = f => f.options.map(o => `
    <label class="filter-option">
      <input type="checkbox" value="${escapeHtml(o.value)}">
      <span class="filter-option__box"><i data-lucide="check"></i></span>
      <span class="filter-option__text">${escapeHtml(o.label)}</span>
    </label>`).join("");

  const datePopoverHtml = () => `
    <div class="filter-presets">
      ${DATE_PRESETS.map((p, i) => `<button type="button" class="btn filter-preset" data-preset="${i}">${p.label}</button>`).join("")}
    </div>
    <div class="filter-dates">
      <label>Dari <input type="date" data-range="from"></label>
      <label>Sampai <input type="date" data-range="to"></label>
    </div>`;

  const toolbarHtml = actions => actions.map(a => {
    const cls = `btn btn-${escapeHtml(a.variant ?? "primary")} table-toolbar-btn`;
    const icon = a.icon ? `<i data-lucide="${escapeHtml(a.icon)}"></i>` : "";
    const body = `${icon}${escapeHtml(a.label)}`;

    if (a.href) return `<a href="${escapeHtml(a.href)}" class="${cls}">${body}</a>`;

    const toggle = a.toggle
      ? ` data-bs-toggle="${escapeHtml(a.toggle)}" data-bs-target="${escapeHtml(a.target ?? "")}"` : "";

    return `<button type="button" class="${cls}" data-action="${escapeHtml(a.name ?? slug(a.label))}"${toggle}>${body}</button>`;
  }).join("");

  function actionsHtml(actions, row, idKey) {
    const id = row?.[idKey];

    const items = actions.map(action => {
      const name = action.name ?? slug(action.label);
      const classes = `btn table-action${action.variant ? ` table-action--${escapeHtml(action.variant)}` : ""}`;
      const icon = action.icon ? `<i data-lucide="${escapeHtml(action.icon)}"></i>` : "";
      const body = `${icon}${escapeHtml(action.label)}`;

      if (Array.isArray(action.items) && action.items.length) {
        const current = action.currentKey ? optionKey(row?.[action.currentKey]) : null;
        const menu = action.items.map(item => `
          <li><button type="button"
            class="dropdown-item${optionKey(item.value) === current ? " active disabled" : ""}"
            data-action="${escapeHtml(name)}" data-id="${escapeHtml(id)}" data-value="${escapeHtml(item.value)}">
            ${escapeHtml(item.label ?? item.value)}</button></li>`).join("");

        return `
          <div class="dropdown">
            <button type="button" class="${classes} dropdown-toggle" data-bs-toggle="dropdown"
              data-bs-popper-config='{"strategy":"fixed"}' aria-expanded="false">${body}</button>
            <ul class="dropdown-menu dropdown-menu-end">${menu}</ul>
          </div>`;
      }

      const toggle = action.toggle
        ? ` data-bs-toggle="${escapeHtml(action.toggle)}" data-bs-target="${escapeHtml(action.target ?? "")}"` : "";
      const attrs = `data-action="${escapeHtml(name)}" data-id="${escapeHtml(id)}"${toggle}`;

      return action.href
        ? `<a href="${escapeHtml(fillTemplate(action.href, row))}" class="${classes}" ${attrs}>${body}</a>`
        : `<button type="button" class="${classes}" ${attrs}>${body}</button>`;
    });

    return `<div class="table-actions grid-card__actions">${items.join("")}</div>`;
  }

  function badgeHtml(cfg, key, value) {
    if (!key || isBlank(value)) return "";

    const k = optionKey(value);
    const conf = cfg.badges?.[key];
    const label = lowerKeys(conf?.labels)[k] ?? (typeof value === "boolean" ? boolLabel(value) : String(value));
    const variant = lowerKeys(conf?.variants)[k] ?? (typeof value === "boolean" ? (value ? "active" : "inactive") : slug(k));

    return `<span class="status-badge status-badge--${escapeHtml(variant)}">${escapeHtml(label)}</span>`;
  }

  function cardHtml(cfg, card, row, actions) {
    const title = escapeHtml(row?.[card.title]);
    const img = card.image ? row?.[card.image] : null;

    const media = img
      ? `<button type="button" class="grid-card__preview" ${cfg.preview ? "data-grid-preview" : "tabindex=\"-1\""}
           data-src="${escapeHtml(img)}" data-title="${title}" aria-label="Lihat gambar ${title}">
           <img src="${escapeHtml(img)}" alt="${title}" loading="lazy"></button>`
      : `<div class="grid-card__placeholder"><i data-lucide="image-off"></i></div>`;

    const meta = card.meta.length
      ? `<dl class="grid-card__meta">${card.meta.map(k => `
          <div><dt>${escapeHtml(cfg.labels[k] ?? DEFAULT_LABELS[k] ?? humanize(k))}</dt>
          <dd>${escapeHtml(formatValue(k, row?.[k]))}</dd></div>`).join("")}</dl>`
      : "";

    return `
      <article class="grid-card" data-id="${escapeHtml(row?.[cfg.idKey])}">
        <div class="grid-card__media">
          ${media}
          ${badgeHtml(cfg, card.badge, row?.[card.badge])}
        </div>
        <div class="grid-card__body">
          <h3 class="grid-card__title">${title}</h3>
          ${card.subtitle && !isBlank(row?.[card.subtitle]) ? `<p class="grid-card__subtitle">${escapeHtml(row[card.subtitle])}</p>` : ""}
          ${card.description && !isBlank(row?.[card.description]) ? `<p class="grid-card__desc">${escapeHtml(row[card.description])}</p>` : ""}
          ${meta}
        </div>
        ${cfg.cardHref ? `
          <a class="grid-card__go" href="${escapeHtml(fillTemplate(cfg.cardHref, row))}"
             aria-label="Buka ${title}">
            <i data-lucide="arrow-up-right"></i>
          </a>` : ""}
        ${actions.length ? actionsHtml(actions, row, cfg.idKey) : ""}
      </article>`;
  }

  function pagerHtml(page, pages) {
    const btn = (label, target, cls = "", off = false) =>
      `<button type="button" class="dt-paging-button ${cls}${off ? " disabled" : ""}" data-page="${target}"${off ? " disabled" : ""}>${label}</button>`;

    const from = Math.max(1, page - 2);
    const to = Math.min(pages, page + 2);
    let html = btn("«", 1, "", page === 1) + btn('<i data-lucide="chevron-left"></i>', page - 1, "", page === 1);

    for (let p = from; p <= to; p++) html += btn(p, p, p === page ? "current" : "");

    return html + btn('<i data-lucide="chevron-right"></i>', page + 1, "", page === pages) + btn("»", pages, "", page === pages);
  }

  // ---------- lightbox ----------
  function openPreview(src, title) {
    let dlg = document.querySelector("dialog.grid-lightbox");

    if (!dlg) {
      dlg = document.createElement("dialog");
      dlg.className = "grid-lightbox";
      dlg.addEventListener("click", e => { if (e.target === dlg) dlg.close(); });
      document.body.appendChild(dlg);
    }

    dlg.innerHTML = `
      <form method="dialog" class="grid-lightbox__bar">
        <span>${escapeHtml(title)}</span>
        <button class="btn-close" aria-label="Tutup"></button>
      </form>
      <img src="${escapeHtml(src)}" alt="${escapeHtml(title)}">`;

    dlg.showModal();
  }

  // ---------- init ----------
  function init(root, overrides = {}) {
    if (root.__appDataGrid) return root.__appDataGrid;

    const rows = unwrapRows(overrides.data ?? readJson(root, "[data-grid-data]", []));
    const cfg = { ...DEFAULTS, ...readJson(root, "[data-grid-config]", {}), ...overrides };
    const card = resolveCard(rows, cfg.card ?? {});
    const schema = normalizeSchema(readJson(root, "[data-grid-schema]", []));

    const actions = cfg.actions
      ?? (cfg.detailUrl ? [{ name: "detail", label: "Detail", icon: "eye", href: cfg.detailUrl }] : []);

    const filters = resolveFilters(rows, cfg, schema);
    const fstate = createFilterState(filters);
    const toolbar = Array.isArray(cfg.toolbar) ? cfg.toolbar : [];

    const mount = root.querySelector("[data-grid-mount]") ?? root;
    root.style.setProperty("--grid-min", `${cfg.minCardWidth}px`);
    root.style.setProperty("--grid-aspect", cfg.aspect);

    mount.innerHTML = `
      <div class="filter-bar">
        <label class="table-search">
          <input type="search" placeholder="${escapeHtml(cfg.searchPlaceholder)}" aria-label="Cari">
        </label>
        ${filters.map(f => f.kind === "set"
          ? chipHtml(f, setPopoverHtml(f))
          : chipHtml(f, datePopoverHtml(), "filter-popover--dates")).join("")}
        <div class="filter-bar__meta">
          <span class="filter-count" aria-live="polite"></span>
          <button type="button" class="filter-clear-all" hidden>Hapus semua</button>
          ${toolbarHtml(toolbar)}
        </div>
      </div>
      <div data-grid-body></div>
      <div class="grid-footer"><div class="dt-info"></div><div class="dt-paging"></div></div>`;

    const bar = mount.querySelector(".filter-bar");
    const body = mount.querySelector("[data-grid-body]");
    const info = mount.querySelector(".dt-info");
    const pager = mount.querySelector(".dt-paging");
    const chips = [...bar.querySelectorAll(".filter-chip")];
    const filterOf = chip => filters.find(f => f.key === chip.dataset.filter);

    const inst = { root, cfg, filters, rows, view: [], page: 1, query: "" };

    const haystack = r =>
      (cfg.searchKeys ?? Object.keys(r ?? {}).filter(k => !/id$/i.test(k)))
        .map(k => r?.[k])
        .filter(v => v !== null && v !== undefined && typeof v !== "object")
        .join(" ").toLowerCase();

    function sorted(list) {
      const s = cfg.defaultSort;
      if (!s || !s.key || !list.some(r => s.key in (r ?? {}))) return list;

      const dir = s.dir === "asc" ? 1 : -1;
      const val = r => (ISO_DATE.test(String(r?.[s.key] ?? "")) ? toTime(r[s.key]) : r?.[s.key]);

      return [...list].sort((a, b) => {
        const x = val(a), y = val(b);
        return (x > y ? 1 : x < y ? -1 : 0) * dir;
      });
    }

    function compute() {
      const q = inst.query.trim().toLowerCase();
      let list = inst.rows.filter(r => matchesFilters(filters, fstate, r));
      if (q) list = list.filter(r => haystack(r).includes(q));
      inst.view = sorted(list);
    }

    function closeAll() {
      chips.forEach(chip => {
        chip.querySelector(".filter-popover").hidden = true;
        chip.querySelector(".filter-chip__trigger").setAttribute("aria-expanded", "false");
      });
    }

    function refreshChips() {
      let anyActive = false;

      chips.forEach(chip => {
        const f = filterOf(chip);
        const cur = fstate[f.key];

        if (f.kind === "set") {
          chip.querySelectorAll('input[type="checkbox"]').forEach(cb => { cb.checked = cur.has(cb.value); });
        } else {
          chip.querySelectorAll('input[type="date"]').forEach(i => { i.value = cur[i.dataset.range]; });
          chip.querySelectorAll(".filter-preset").forEach(btn => {
            const p = presetRange(DATE_PRESETS[Number(btn.dataset.preset)]);
            btn.classList.toggle("is-active", p.from === cur.from && p.to === cur.to);
          });
        }

        const value = chipValue(f, fstate);
        const active = value !== "";
        anyActive = anyActive || active;

        chip.classList.toggle("is-active", active);
        const el = chip.querySelector(".filter-chip__value");
        el.textContent = value;
        el.hidden = !active;
        chip.querySelector(".filter-chip__clear").hidden = !active;
      });

      bar.querySelector(".filter-clear-all").hidden = !anyActive;
    }

    function render() {
      compute();

      const total = inst.view.length;
      const pages = Math.max(1, Math.ceil(total / cfg.pageSize));
      inst.page = Math.min(inst.page, pages);

      const start = (inst.page - 1) * cfg.pageSize;
      const slice = inst.view.slice(start, start + cfg.pageSize);

      bar.querySelector(".filter-count").textContent = `${total} hasil`;

      body.innerHTML = slice.length
        ? `<div class="grid-cards">${slice.map(r => cardHtml(cfg, card, r, actions)).join("")}</div>`
        : `<div class="grid-empty"><i data-lucide="image-off"></i><span>${escapeHtml(inst.rows.length ? cfg.zeroText : cfg.emptyText)}</span></div>`;

      info.textContent = total
        ? `Menampilkan ${start + 1}–${start + slice.length} dari ${total} data`
        : "Tidak ada data";

      pager.innerHTML = pages > 1 ? pagerHtml(inst.page, pages) : "";
      renderIcons(root);
    }

    function apply() {
      inst.page = 1;
      refreshChips();
      render();
    }

    // search
    bar.querySelector(".table-search input").addEventListener("input", e => {
      inst.query = e.target.value;
      inst.page = 1;
      render();
    });

    // klik toolbar: popover, hapus chip, preset, hapus semua
    bar.addEventListener("click", e => {
      const chip = e.target.closest(".filter-chip");
      const trigger = e.target.closest(".filter-chip__trigger");
      const clear = e.target.closest(".filter-chip__clear");
      const preset = e.target.closest(".filter-preset");
      const clearAll = e.target.closest(".filter-clear-all");

      if (trigger && chip) {
        const pop = chip.querySelector(".filter-popover");
        const willOpen = pop.hidden;
        closeAll();
        pop.hidden = !willOpen;
        trigger.setAttribute("aria-expanded", String(willOpen));
        return;
      }

      if (clear && chip) { resetFilter(filterOf(chip), fstate); apply(); return; }

      if (preset && chip) {
        fstate[chip.dataset.filter] = presetRange(DATE_PRESETS[Number(preset.dataset.preset)]);
        apply();
        return;
      }

      if (clearAll) { filters.forEach(f => resetFilter(f, fstate)); closeAll(); apply(); }
    });

    bar.addEventListener("change", e => {
      const chip = e.target.closest(".filter-chip");
      if (!chip) return;

      const f = filterOf(chip);

      if (f.kind === "set") {
        if (e.target.checked) fstate[f.key].add(e.target.value);
        else fstate[f.key].delete(e.target.value);
      } else if (e.target.type === "date") {
        fstate[f.key][e.target.dataset.range] = e.target.value;
      }

      apply();
    });

    document.addEventListener("click", e => { if (!bar.contains(e.target)) closeAll(); });
    document.addEventListener("keydown", e => { if (e.key === "Escape") closeAll(); });

    // paginasi
    pager.addEventListener("click", e => {
      const btn = e.target.closest("[data-page]");
      if (!btn || btn.disabled) return;
      inst.page = Number(btn.dataset.page);
      render();
      root.scrollIntoView({ block: "start", behavior: "smooth" });
    });

    // preview gambar + aksi
    root.addEventListener("click", e => {
      const prev = e.target.closest("[data-grid-preview]");
      if (prev) { openPreview(prev.dataset.src, prev.dataset.title); return; }

      const el = e.target.closest("[data-action]");
      if (!el || !root.contains(el) || el.tagName === "A") return;

      root.dispatchEvent(new CustomEvent("datagrid:action", {
        bubbles: true,
        detail: {
          action: el.dataset.action,
          id: el.dataset.id ?? null,
          value: el.dataset.value ?? null,
          row: el.dataset.id
            ? inst.rows.find(r => String(r?.[cfg.idKey]) === el.dataset.id) ?? null
            : null
        }
      }));
    });

    inst.reload = payload => { inst.rows = unwrapRows(payload); apply(); };

    refreshChips();
    render();

    root.__appDataGrid = inst;
    return inst;
  }

  function initAll() {
    document.querySelectorAll("[data-grid-root]").forEach(el => init(el));
  }

  window.AppDataGrid = {
    init,
    initAll,
    get: t => (typeof t === "string" ? document.getElementById(t) : t)?.__appDataGrid ?? null
  };

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", initAll);
  else initAll();
})();