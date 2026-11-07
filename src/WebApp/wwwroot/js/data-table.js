/*!
 * data-table.js
 * Wrapper DataTables yang reusable: kolom, badge, filter, dan aksi dibentuk otomatis dari data + schema model.
 * Dipakai lewat partial _DataTable.cshtml, atau manual: AppDataTable.init(element, options).
 *
 * Sumber informasi kolom (urutan prioritas):
 *   1. config.columns            -> eksplisit dari halaman
 *   2. schema (data-dt-schema)   -> dibaca partial dari tipe model C# (tetap ada walau data kosong)
 *   3. isi data                  -> fallback / pelengkap
 */
(() => {
  "use strict";

  // ==================================================
  // Config
  // ==================================================
  const LOCALE = "id-ID";

  const DEFAULTS = {
    idKey: "id",
    pageLength: 25,
    lengthMenu: [10, 25, 50],
    scrollY: null,           // tinggi area tabel (px). Tabel scroll vertikal di dalamnya
    scrollCollapse: true,   // true = tinggi mengikuti isi sampai batas scrollY
    searchPlaceholder: "Cari...",
    emptyText: "Belum ada data",
    zeroText: "Data tidak ditemukan",
    actionsTitle: "Aksi",
    truncateAt: 60,
    filters: true,      // true = otomatis (kolom badge + tanggal) | false = mati | ["status", "createdAt"]
    stack: null,        // { customerName: "email" } -> dua baris dalam satu sel. null = deteksi otomatis
    hide: [],           // key yang disembunyikan
    show: [],           // key yang biasanya disembunyikan (id, *Id, updatedAt) tapi mau ditampilkan
    columns: null,      // ["customerName", { key: "price", title: "Harga", type: "currency" }]
    schema: null,       // [{ key, type, options }] -> diisi otomatis oleh partial dari tipe model
    labels: {},         // { customerName: "Pelanggan" }
    badges: {},         // { status: { labels: { pending: "Menunggu" }, variants: { pending: "pending" } } }
    actions: null,      // [{ name, label, icon, href: "/x?id={id}", toggle, target, variant: "accent" }]
    detailUrl: null,    // shortcut satu aksi "Detail"
    toolbar: null,      // [{ label, icon, toggle: "offcanvas"|"modal", target: "#id", href, variant, name }]
    defaultSort: null   // { key: "createdAt", dir: "desc" } | false
  };

  const DEFAULT_LABELS = {
    id: "ID",
    name: "Nama",
    customerName: "Pelanggan",
    email: "Email",
    phoneNumber: "Telepon",
    phone: "Telepon",
    message: "Pesan",
    description: "Deskripsi",
    inclusions: "Fasilitas",
    status: "Status",
    slug: "Slug",
    price: "Harga",
    durationInMinutes: "Durasi (menit)",
    maxParticipantText: "Maks. peserta per sesi",
    preferredDate: "Tanggal Booking",
    createdAt: "Dibuat",
    updatedAt: "Terakhir Diperbarui",
    isActive: "Status",
    imageUrl: "Gambar",
    photographyPackageName: "Paket",
    packagePrice: "Harga Paket",
    inquiryCountText: "Jumlah Pesanan",
    isPublished: "Status Publikasi", 
    displayOrder: "Urutan Tampil",
    invoiceCode: "Kode Invoice",
    customerEmail: "Email pelanggan",
    customerPhoneNumber: "Nomor Telepon Pelanggan",
    packageName: "Nama Paket",
    issuedAt: "Diterbitkan Pada",
    totalInvoice: "Total Invoice",
    theme: "Tema",
    totalPrize: "Total Hadiah",
    isEligible: "Status Kelayakan",
    registrationPeriod: "Masa Pendaftaran",
    relatedCompetitionText: "Lomba Terkait",
    relatedScholarshipText: "Beasiswa Terkait",
    totalPrizeText: "Total Hadiah"
  };

  const DATE_PRESETS = [
    { label: "Hari ini", days: 0 },
    { label: "7 hari terakhir", days: 6 },
    { label: "30 hari terakhir", days: 29 },
    { label: "Bulan ini", month: true }
  ];

  const ISO_DATE = /^\d{4}-\d{2}-\d{2}(?:[T ]\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:?\d{2})?)?$/;
  const STATUS_KEY = /status$|^state$/i;
  const LONGTEXT_KEY = /message|description|note|content|inclusions|address/i;
  const CURRENCY_KEY = /price|harga|amount|fee|cost|salary/i;

  const currencyFmt = new Intl.NumberFormat(LOCALE, {
    style: "currency",
    currency: "IDR",
    maximumFractionDigits: 0
  });
  const numberFmt = new Intl.NumberFormat(LOCALE);

  // ==================================================
  // Helpers
  // ==================================================
  const pad = n => String(n).padStart(2, "0");

  const toKey = d =>
    `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

  const isBlank = v => v === null || v === undefined || v === "";

  const isPlainObject = v =>
    v !== null && typeof v === "object" && !Array.isArray(v);

  const escapeHtml = value =>
    String(value ?? "").replace(/[&<>"']/g, c => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
    }[c]));

  const slug = value =>
    String(value ?? "").toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "");

  const humanize = key => {
    const text = String(key)
      .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
      .replace(/[_-]+/g, " ");
    return text.charAt(0).toUpperCase() + text.slice(1);
  };

  const formatDate = value => {
    if (isBlank(value)) return "-";
    const d = new Date(value);
    return Number.isNaN(d.getTime())
      ? "-"
      : d.toLocaleDateString(LOCALE, { day: "numeric", month: "short", year: "numeric" });
  };

  const formatKey = key =>
    new Date(key + "T00:00:00").toLocaleDateString(LOCALE, {
      day: "numeric", month: "short"
    });

  const toTime = value => {
    const t = isBlank(value) ? NaN : new Date(value).getTime();
    return Number.isNaN(t) ? 0 : t;
  };

  const fillTemplate = (template, row) =>
    String(template).replace(/\{(\w+)\}/g, (_, k) => encodeURIComponent(row?.[k] ?? ""));

  const optionKey = v => String(v ?? "").toLowerCase();

  const lowerKeys = obj =>
    Object.fromEntries(Object.entries(obj ?? {}).map(([k, v]) => [optionKey(k), v]));

  const cssSize = v => (typeof v === "number" ? `${v}px` : String(v));

  function readJson(root, selector, fallback) {
    const el = root.querySelector(selector);
    if (!el) return fallback;

    try {
      return JSON.parse(el.textContent || "") ?? fallback;
    } catch (err) {
      console.error(`[data-table] JSON ${selector} tidak valid:`, err);
      return fallback;
    }
  }

  // Terima array langsung, atau response berbentuk { items: [...], page, pageSize, totalCount }
  function unwrapRows(payload) {
    if (Array.isArray(payload)) return payload;
    if (payload && Array.isArray(payload.items)) return payload.items;
    if (payload && Array.isArray(payload.data)) return payload.data;
    return [];
  }

  function renderIcons(root) {
    if (typeof lucide === "undefined") return;
    lucide.createIcons({ root });
  }

  // ==================================================
  // Schema (info kolom dari tipe model, tidak bergantung pada isi data)
  // ==================================================
  // [{ key, type: "text|number|boolean|date|list|object|enum", options: [{ value, label }] }]
  function normalizeSchema(raw) {
    const map = new Map();

    (Array.isArray(raw) ? raw : []).forEach(item => {
      if (!item || !item.key) return;
      map.set(item.key, {
        type: item.type ?? "text",
        options: Array.isArray(item.options) ? item.options : []
      });
    });

    return map;
  }

  // ==================================================
  // Columns (inference)
  // ==================================================
  function collectKeys(rows, schema) {
    // urutan schema = urutan properti di C#, jadi kolom stabil walau ada properti null yang tidak ikut ter-serialize
    const keys = [...schema.keys()];
    const seen = new Set(keys);

    rows.slice(0, 50).forEach(row => {
      Object.keys(row ?? {}).forEach(k => {
        if (seen.has(k)) return;
        seen.add(k);
        keys.push(k);
      });
    });

    return keys;
  }

  const isHiddenKey = k =>
    /^id$/i.test(k) || /[a-z]Id$/.test(k) || /^updatedAt$/i.test(k);

  // tebakan dari nama properti, dipakai saat belum ada nilai sama sekali
  function guessByName(key) {
    if (STATUS_KEY.test(key)) return "badge";
    if (/(At|Date|Time)$/.test(key)) return "date";
    if (CURRENCY_KEY.test(key)) return "currency";
    return "text";
  }

  function detectType(key, values, hint) {
    // schema dari model C# paling bisa dipercaya
    if (hint?.type === "enum") return "badge";
    if (hint?.type === "boolean") return "boolean";

    const sample = values.find(v => !isBlank(v));

    // belum ada nilai (data kosong / kolom null semua)
    if (sample === undefined) {
      switch (hint?.type) {
        case "date": return "date";
        case "list": return "list";
        case "object": return "object";
        case "number":
          return STATUS_KEY.test(key) ? "badge" : CURRENCY_KEY.test(key) ? "currency" : "number";
        default: return guessByName(key);
      }
    }

    if (typeof sample === "boolean") return "boolean";

    if (typeof sample === "number") {
      if (STATUS_KEY.test(key)) return "badge";
      return CURRENCY_KEY.test(key) ? "currency" : "number";
    }

    if (Array.isArray(sample)) return "list";
    if (isPlainObject(sample)) return "object";

    if (typeof sample === "string") {
      if (ISO_DATE.test(sample)) return "date";
      if (STATUS_KEY.test(key)) return "badge";

      const longest = values.reduce(
        (max, v) => (typeof v === "string" ? Math.max(max, v.length) : max), 0);

      if (LONGTEXT_KEY.test(key) || longest > 80) return "longtext";
    }

    return "text";
  }

  const normalizeBadge = badge => ({
    labels: lowerKeys(badge?.labels),
    variants: lowerKeys(badge?.variants)
  });

  function resolveColumns(rows, cfg, schema) {
    const keys = collectKeys(rows, schema);

    const specs = Array.isArray(cfg.columns) && cfg.columns.length
      ? cfg.columns.map(c => (typeof c === "string" ? { key: c } : { ...c }))
      : null;

    // stack: nama + email dalam satu sel
    let stack = cfg.stack;
    if (stack === null || stack === undefined) {
      const nameKey = keys.find(k => /name$/i.test(k));
      stack = nameKey && keys.includes("email") ? { [nameKey]: "email" } : {};
    } else if (stack === false) {
      stack = {};
    }

    const stackedSubs = new Set(Object.values(stack));
    const hide = new Set(cfg.hide);
    const show = new Set(cfg.show);

    const base = specs ?? keys
      .filter(k => !stackedSubs.has(k))
      .filter(k => show.has(k) || !isHiddenKey(k))
      .filter(k => !hide.has(k))
      .map(k => ({ key: k }));

    return base
      .map(spec => {
        const key = spec.key;
        const values = rows.map(r => r?.[key]);
        const hint = schema.get(key);
        const options = hint?.options ?? [];
        const type = spec.type ?? detectType(key, values, hint);

        return {
          key,
          type,
          title: spec.title ?? cfg.labels[key] ?? DEFAULT_LABELS[key] ?? humanize(key),
          stack: spec.stack ?? stack[key] ?? null,
          className: spec.className ?? null,
          orderable: spec.orderable !== false,
          searchable: spec.searchable !== false,
          filter: spec.filter,
          badge: normalizeBadge(spec.badge ?? cfg.badges?.[key]),
          options,
          optionLabels: new Map(options.map(o => [optionKey(o.value), humanize(o.label ?? o.value)])),
          render: spec.render ?? null
        };
      })
      .filter(col => specs || col.type !== "object");
  }

  // class sel: className dari config + rata kanan kolom angka + kolom teks panjang boleh wrap
  const cellClass = col => [
    col.className,
    col.type === "number" || col.type === "currency" ? "dt-align-end" : "",
    col.type === "longtext" ? "dt-col-longtext" : ""
  ].filter(Boolean).join(" ");

  // ==================================================
  // Renderers
  // ==================================================
  // urutan: label dari config -> label enum dari model -> nilai mentah
  const badgeLabel = (col, value) => {
    const k = optionKey(value);
    return col.badge.labels[k] ?? col.optionLabels.get(k) ?? String(value ?? "-");
  };

  const boolLabel = value => (value ? "Aktif" : "Nonaktif");

  function buildRenderer(col, cfg) {
    if (typeof col.render === "function") return col.render;

    const dash = '<span class="text-muted">-</span>';

    switch (col.type) {
      case "date":
        return (data, kind) => {
          if (kind === "display") return formatDate(data);
          if (kind === "filter") return isBlank(data) ? "" : formatDate(data);
          return toTime(data);
        };

      case "number":
      case "currency": {
        const fmt = col.type === "currency" ? currencyFmt : numberFmt;
        return (data, kind) => {
          if (kind === "display") return isBlank(data) ? dash : fmt.format(Number(data));
          if (kind === "filter") return isBlank(data) ? "" : `${data} ${fmt.format(Number(data))}`;
          return isBlank(data) ? 0 : Number(data);
        };
      }

      case "boolean":
        return (data, kind) => {
          if (isBlank(data)) return kind === "display" ? dash : "";
          if (kind !== "display") return boolLabel(data);
          return `<span class="status-badge status-badge--${data ? "active" : "inactive"}">${boolLabel(data)}</span>`;
        };

      case "badge":
        return (data, kind) => {
          if (isBlank(data)) return kind === "display" ? dash : "";

          const label = badgeLabel(col, data);
          if (kind !== "display") return label;

          const key = optionKey(data);
          const variant = col.badge.variants[key] ?? slug(key);

          return `<span class="status-badge status-badge--${escapeHtml(variant)}">${escapeHtml(label)}</span>`;
        };

      case "list":
        return (data, kind) => {
          const text = Array.isArray(data) ? data.join(", ") : String(data ?? "");
          if (kind !== "display") return text;
          return text ? escapeHtml(text) : dash;
        };

      case "longtext":
        return (data, kind) => {
          const text = String(data ?? "");
          if (kind !== "display") return text;
          if (!text) return dash;

          const short = text.length > cfg.truncateAt
            ? text.slice(0, cfg.truncateAt) + "…"
            : text;

          return `<span title="${escapeHtml(text)}">${escapeHtml(short)}</span>`;
        };

      default:
        return (data, kind, row) => {
          const sub = col.stack ? row?.[col.stack] : null;

          if (kind !== "display") {
            return col.stack ? `${data ?? ""} ${sub ?? ""}`.trim() : (data ?? "");
          }

          if (col.stack) {
            return `
              <div class="d-flex flex-column">
                <span class="fw-semibold">${escapeHtml(data)}</span>
                <span class="text-muted small">${escapeHtml(sub)}</span>
              </div>
            `;
          }

          return isBlank(data) ? dash : escapeHtml(data);
        };
    }
  }

  function buildActionsRenderer(actions, idKey) {
    return (data, kind, row) => {
      if (kind !== "display") return "";

      const id = row?.[idKey];

      const items = actions.map(action => {
        const name = action.name ?? slug(action.label);
        const classes = `btn table-action${action.variant ? ` table-action--${escapeHtml(action.variant)}` : ""}`;
        const icon = action.icon ? `<i data-lucide="${escapeHtml(action.icon)}"></i>` : "";
        const body = `${icon}${escapeHtml(action.label)}`;

        // aksi dropdown: action.items = [{ value, label }]
        if (Array.isArray(action.items) && action.items.length) {
          const current = action.currentKey ? optionKey(row?.[action.currentKey]) : null;

          const menu = action.items.map(item => `
            <li>
              <button type="button"
                class="dropdown-item${optionKey(item.value) === current ? " active disabled" : ""}"
                data-action="${escapeHtml(name)}"
                data-id="${escapeHtml(id)}"
                data-value="${escapeHtml(item.value)}">
                ${escapeHtml(item.label)}
              </button>
            </li>
          `).join("");

          return `
            <div class="dropdown">
              <button type="button" class="${classes} dropdown-toggle"
                data-bs-toggle="dropdown"
                data-bs-popper-config='{"strategy":"fixed"}'
                aria-expanded="false">${body}</button>
              <ul class="dropdown-menu dropdown-menu-end">${menu}</ul>
            </div>
          `;
        }

        const toggle = action.toggle
          ? ` data-bs-toggle="${escapeHtml(action.toggle)}" data-bs-target="${escapeHtml(action.target ?? "")}"`
          : "";
        const attrs = `data-action="${escapeHtml(name)}" data-id="${escapeHtml(id)}"${toggle}`;

        return action.href
          ? `<a href="${escapeHtml(fillTemplate(action.href, row))}" class="${classes}" ${attrs}>${body}</a>`
          : `<button type="button" class="${classes}" ${attrs}>${body}</button>`;
      });

      return `<div class="table-actions">${items.join("")}</div>`;
    };
  }

  // ==================================================
  // Filters (resolve + state + markup)
  // ==================================================
  function optionLabel(col, value) {
    if (col.type === "badge") return badgeLabel(col, value);
    if (col.type === "boolean") return boolLabel(value);
    return String(value);
  }

  // Opsi filter dikumpulkan dari: enum model -> label di config -> isi data.
  // Jadi chip filter tetap punya pilihan walau data masih kosong.
  function optionsFor(col, rows) {
    const map = new Map();

    const add = value => {
      if (isBlank(value)) return;
      const key = optionKey(value);
      if (!map.has(key)) map.set(key, optionLabel(col, value));
    };

    if (col.type === "boolean") {
      add(true);
      add(false);
    }

    col.options.forEach(o => add(o.value));
    Object.keys(col.badge.labels).forEach(add);
    rows.forEach(row => add(row?.[col.key]));

    return [...map].map(([value, label]) => ({ value, label }));
  }

  function resolveFilters(columns, rows, cfg) {
    if (cfg.filters === false) return [];

    const wanted = Array.isArray(cfg.filters) ? cfg.filters : null;

    const pool = wanted
      ? wanted.map(key => columns.find(c => c.key === key)).filter(Boolean)
      : columns.filter(c => c.type === "badge" || c.type === "date");

    return pool
      .filter(col => col.filter !== false)
      .map(col => col.type === "date"
        ? { key: col.key, title: col.title, kind: "range" }
        : { key: col.key, title: col.title, kind: "set", options: optionsFor(col, rows) })
      .filter(f => f.kind === "range" || f.options.length);
  }

  function createFilterState(filters) {
    const state = {};
    filters.forEach(f => {
      state[f.key] = f.kind === "set" ? new Set() : { from: "", to: "" };
    });
    return state;
  }

  function presetRange(preset) {
    const today = new Date();
    const to = toKey(today);

    if (preset.month) {
      return {
        from: toKey(new Date(today.getFullYear(), today.getMonth(), 1)),
        to
      };
    }

    const from = new Date(today);
    from.setDate(from.getDate() - preset.days);
    return { from: toKey(from), to };
  }

  function chipValue(filter, state) {
    const current = state[filter.key];

    if (filter.kind === "set") {
      const labels = filter.options
        .filter(o => current.has(o.value))
        .map(o => o.label);

      if (!labels.length) return "";
      return labels.length > 1
        ? `${labels[0]}, +${labels.length - 1}`
        : labels[0];
    }

    const { from, to } = current;
    if (!from && !to) return "";

    if (from && to) {
      return from === to
        ? formatKey(from)
        : `${formatKey(from)} – ${formatKey(to)}`;
    }

    return from ? `Sejak ${formatKey(from)}` : `Hingga ${formatKey(to)}`;
  }

  function resetFilter(filter, state) {
    if (filter.kind === "set") state[filter.key].clear();
    else state[filter.key] = { from: "", to: "" };
  }

  function matchesFilters(filters, state, row) {
    for (const filter of filters) {
      const current = state[filter.key];

      if (filter.kind === "set") {
        if (current.size && !current.has(optionKey(row?.[filter.key]))) return false;
        continue;
      }

      const { from, to } = current;
      if (!from && !to) continue;
      if (isBlank(row?.[filter.key])) return false;

      const rowKey = toKey(new Date(row[filter.key]));
      if (from && rowKey < from) return false;
      if (to && rowKey > to) return false;
    }

    return true;
  }

  // ---- markup ----
  function chipHtml(filter, popoverHtml, popoverClass = "") {
    const label = escapeHtml(filter.title);

    return `
      <div class="filter-chip" data-filter="${escapeHtml(filter.key)}">
        <div class="filter-chip__main">
          <button type="button" class="btn filter-chip__trigger"
            aria-haspopup="true" aria-expanded="false">
            <span class="filter-chip__label">${label}</span>
            <span class="filter-chip__value" hidden></span>
            <i data-lucide="chevron-down" class="filter-chip__chevron"></i>
          </button>
          <button type="button" class="btn filter-chip__clear"
            aria-label="Hapus filter ${label}" hidden>
            <i data-lucide="x"></i>
          </button>
        </div>
        <div class="filter-popover ${popoverClass}" hidden>
          ${popoverHtml}
        </div>
      </div>
    `;
  }

  const setPopoverHtml = filter =>
    filter.options.map(o => `
      <label class="filter-option">
        <input type="checkbox" value="${escapeHtml(o.value)}">
        <span class="filter-option__box"><i data-lucide="check"></i></span>
        <span class="filter-option__text">${escapeHtml(o.label)}</span>
      </label>
    `).join("");

  const datePopoverHtml = () => `
    <div class="filter-presets">
      ${DATE_PRESETS.map((p, i) => `
        <button type="button" class="btn filter-preset" data-preset="${i}">${p.label}</button>
      `).join("")}
    </div>
    <div class="filter-dates">
      <label>Dari <input type="date" data-range="from"></label>
      <label>Sampai <input type="date" data-range="to"></label>
    </div>
  `;

  // tombol toolbar: link (href) atau tombol yang membuka offcanvas/modal Bootstrap (toggle + target).
  // Tombol tanpa href juga memicu event "datatable:action" (row = null).
  const toolbarHtml = actions => actions.map(a => {
    const cls = `btn btn-${escapeHtml(a.variant ?? "primary")} table-toolbar-btn`;
    const icon = a.icon ? `<i data-lucide="${escapeHtml(a.icon)}"></i>` : "";
    const body = `${icon}${escapeHtml(a.label)}`;

    if (a.href) return `<a href="${escapeHtml(a.href)}" class="${cls}">${body}</a>`;

    const toggle = a.toggle
      ? ` data-bs-toggle="${escapeHtml(a.toggle)}" data-bs-target="${escapeHtml(a.target ?? "")}"`
      : "";

    return `<button type="button" class="${cls}" data-action="${escapeHtml(a.name ?? slug(a.label))}"${toggle}>${body}</button>`;
  }).join("");

  // ==================================================
  // Toolbar: search + chip filter + jumlah hasil + tombol
  // Dibuat sendiri dan ditaruh tepat di atas .dt-container (tidak bergantung pada layout DataTables).
  // ==================================================
  function buildToolbar(inst) {
    const { table, root, filters, state, cfg } = inst;
    const toolbar = Array.isArray(cfg.toolbar) ? cfg.toolbar : [];

    const bar = document.createElement("div");
    bar.className = "filter-bar";
    bar.innerHTML = `
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
    `;

    const container = root.querySelector(".dt-container");
    if (container && container.parentNode) container.parentNode.insertBefore(bar, container);
    else root.prepend(bar);

    const chips = [...bar.querySelectorAll(".filter-chip")];
    const filterOf = chip => filters.find(f => f.key === chip.dataset.filter);

    function closeAll() {
      chips.forEach(chip => {
        chip.querySelector(".filter-popover").hidden = true;
        chip.querySelector(".filter-chip__trigger").setAttribute("aria-expanded", "false");
      });
    }

    // samakan tampilan (checkbox, input tanggal, preset aktif, teks chip) dengan state
    function refresh() {
      let anyActive = false;

      chips.forEach(chip => {
        const filter = filterOf(chip);
        const current = state[filter.key];

        if (filter.kind === "set") {
          chip.querySelectorAll('input[type="checkbox"]').forEach(cb => {
            cb.checked = current.has(cb.value);
          });
        } else {
          chip.querySelectorAll('input[type="date"]').forEach(input => {
            input.value = current[input.dataset.range];
          });

          chip.querySelectorAll(".filter-preset").forEach(btn => {
            const p = presetRange(DATE_PRESETS[Number(btn.dataset.preset)]);
            btn.classList.toggle("is-active", p.from === current.from && p.to === current.to);
          });
        }

        const value = chipValue(filter, state);
        const active = value !== "";
        anyActive = anyActive || active;

        chip.classList.toggle("is-active", active);

        const valueEl = chip.querySelector(".filter-chip__value");
        valueEl.textContent = value;
        valueEl.hidden = !active;

        chip.querySelector(".filter-chip__clear").hidden = !active;
      });

      bar.querySelector(".filter-clear-all").hidden = !anyActive;
    }

    function updateCount() {
      const total = table.rows({ search: "applied" }).count();
      bar.querySelector(".filter-count").textContent = `${total} hasil`;
    }

    function apply() {
      refresh();
      inst.refilter();
    }

    table.on("draw", updateCount);

    // search
    bar.querySelector(".table-search input").addEventListener("input", e => {
      table.search(e.target.value).draw();
    });

    // klik: buka/tutup popover, hapus chip, preset tanggal, hapus semua
    bar.addEventListener("click", e => {
      const chip = e.target.closest(".filter-chip");
      const trigger = e.target.closest(".filter-chip__trigger");
      const clear = e.target.closest(".filter-chip__clear");
      const preset = e.target.closest(".filter-preset");
      const clearAll = e.target.closest(".filter-clear-all");

      if (trigger && chip) {
        const popover = chip.querySelector(".filter-popover");
        const willOpen = popover.hidden;

        closeAll();
        popover.hidden = !willOpen;
        trigger.setAttribute("aria-expanded", String(willOpen));
        return;
      }

      if (clear && chip) {
        resetFilter(filterOf(chip), state);
        apply();
        return;
      }

      if (preset && chip) {
        state[chip.dataset.filter] = presetRange(DATE_PRESETS[Number(preset.dataset.preset)]);
        apply();
        return;
      }

      if (clearAll) {
        filters.forEach(f => resetFilter(f, state));
        closeAll();
        apply();
      }
    });

    // centang checkbox / ubah tanggal
    bar.addEventListener("change", e => {
      const chip = e.target.closest(".filter-chip");
      if (!chip) return;

      const filter = filterOf(chip);

      if (filter.kind === "set") {
        if (e.target.checked) state[filter.key].add(e.target.value);
        else state[filter.key].delete(e.target.value);
      } else if (e.target.type === "date") {
        state[filter.key][e.target.dataset.range] = e.target.value;
      }

      apply();
    });

    document.addEventListener("click", e => {
      if (!bar.contains(e.target)) closeAll();
    });

    document.addEventListener("keydown", e => {
      if (e.key === "Escape") closeAll();
    });

    refresh();
    updateCount();
    renderIcons(root);
  }

  // ==================================================
  // Init
  // ==================================================
  function resolveOrder(columns, cfg) {
    if (cfg.defaultSort === false) return [];

    const wanted = cfg.defaultSort?.key
      ?? columns.find(c => /^createdAt$/i.test(c.key))?.key;

    const index = columns.findIndex(c => c.key === wanted);
    return index >= 0 ? [[index, cfg.defaultSort?.dir ?? "desc"]] : [];
  }

  function init(root, overrides = {}) {
    if (root.__appDataTable) return root.__appDataTable;

    if (typeof DataTable === "undefined") {
      console.error("[data-table] DataTables belum dimuat.");
      return null;
    }

    const tableNode = root.querySelector("table");
    if (!tableNode) return null;

    const rows = unwrapRows(overrides.data ?? readJson(root, "[data-dt-data]", []));
    const cfg = { ...DEFAULTS, ...readJson(root, "[data-dt-config]", {}), ...overrides };
    const schema = normalizeSchema(cfg.schema ?? readJson(root, "[data-dt-schema]", []));

    let columns = resolveColumns(rows, cfg, schema);

    // benar-benar tidak ada info kolom (bukan dari partial, tanpa data, tanpa config)
    if (!columns.length) {
      console.warn("[data-table] Kolom tidak bisa ditentukan: beri config.columns atau schema.");
      columns = [{
        key: "_empty",
        type: "text",
        title: "Data",
        stack: null,
        className: null,
        orderable: false,
        searchable: false,
        filter: false,
        badge: normalizeBadge(null),
        options: [],
        optionLabels: new Map(),
        render: null
      }];
    }

    // aksi
    const actions = cfg.actions
      ?? (cfg.detailUrl
        ? [{ name: "detail", label: "Detail", icon: "eye", href: cfg.detailUrl }]
        : []);
    const hasActions = actions.length > 0;

    // header
    tableNode.innerHTML = `
      <thead>
        <tr>
          ${columns.map(c => `<th scope="col" class="${cellClass(c)}">${escapeHtml(c.title)}</th>`).join("")}
          ${hasActions ? `<th scope="col" class="dt-actions-col">${escapeHtml(cfg.actionsTitle)}</th>` : ""}
        </tr>
      </thead>
      <tbody></tbody>
    `;

    const dtColumns = columns.map(col => ({
      data: col.key,
      defaultContent: "",
      orderable: col.orderable,
      searchable: col.searchable,
      className: cellClass(col) || undefined,
      render: buildRenderer(col, cfg)
    }));

    if (hasActions) {
      dtColumns.push({
        data: null,
        orderable: false,
        searchable: false,
        className: "dt-actions-col",
        render: buildActionsRenderer(actions, cfg.idKey)
      });
    }

    // filter (dicek di setiap draw lewat ext.search)
    const filters = resolveFilters(columns, rows, cfg);
    const state = createFilterState(filters);

    const table = new DataTable(tableNode, {
      data: rows,

      paging: true,
      ordering: true,
      searching: true,
      order: resolveOrder(columns, cfg),

      pageLength: cfg.pageLength,
      lengthMenu: cfg.lengthMenu,
      processing: false,
      autoWidth: false,

      initComplete: function () {
        const length = root.querySelector(".dt-length");
        const select = length?.querySelector("select");

        if (select && !length.querySelector(".length-select")) {
          select.insertAdjacentHTML("beforebegin", '<span class="length-select"></span>');

          const wrapper = select.previousElementSibling;
          wrapper.appendChild(select);
          wrapper.insertAdjacentHTML("beforeend", '<i data-lucide="chevron-down"></i>');
        }

        renderIcons(root);
      },

      drawCallback: () => renderIcons(root),

      // search + chip dibuat sendiri di buildToolbar, jadi bagian atas dikosongkan
      layout: {
        topStart: null,
        topEnd: null,
        bottomStart: ["pageLength", "info"],
        bottomEnd: "paging"
      },

      columns: dtColumns,

      language: {
        search: "",
        searchPlaceholder: cfg.searchPlaceholder,
        lengthMenu: "Tampilkan _MENU_ data",
        info: "Menampilkan _START_–_END_ dari _TOTAL_ data",
        infoEmpty: "Tidak ada data",
        infoFiltered: "(disaring dari _MAX_ data)",
        emptyTable: cfg.emptyText,
        zeroRecords: cfg.zeroText,
        paginate: {
          previous: '<i data-lucide="chevron-left"></i>',
          next: '<i data-lucide="chevron-right"></i>'
        }
      }
    });

    const instance = {
      root,
      table,
      columns,
      filters,
      state,
      cfg,
      rows,
      refilter() {
        const visible = instance.rows.filter(r => matchesFilters(filters, state, r));
        table.clear().rows.add(visible).draw();
      },
      reload(payload) {
        instance.rows = unwrapRows(payload);
        instance.refilter();
      }
    };

    try {
      buildToolbar(instance);
    } catch (err) {
      console.error("[data-table] Gagal membuat toolbar:", err);
    }

    // Area scroll: tinggi dibatasi (scrollY), scroll ke samping kalau kolom melebihi lebar, header sticky.
    const host = document.createElement("div");
    host.className = "dt-scroll-host";
    if (cfg.scrollY) host.style[cfg.scrollCollapse ? "maxHeight" : "height"] = cssSize(cfg.scrollY);

    tableNode.parentNode.insertBefore(host, tableNode);
    host.appendChild(tableNode);

    // aksi tanpa href (dan tombol toolbar) -> event "datatable:action" (di-listen dari halaman)
    root.addEventListener("click", e => {
      const el = e.target.closest("[data-action]");
      if (!el || !root.contains(el) || el.tagName === "A") return;

      const tr = el.closest("tr");
      root.dispatchEvent(new CustomEvent("datatable:action", {
        bubbles: true,
        detail: {
          action: el.dataset.action,
          id: el.dataset.id,
          value: el.dataset.value ?? null,
          row: tr ? table.row(tr).data() : null
        }
      }));
    });

    root.__appDataTable = instance;
    return instance;
  }

  function initAll() {
    document.querySelectorAll("[data-dt-root]").forEach(el => init(el));
  }

  window.AppDataTable = {
    init,
    initAll,
    get: target => (typeof target === "string"
      ? document.getElementById(target)
      : target)?.__appDataTable ?? null
  };

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", initAll);
  } else {
    initAll();
  }
})();