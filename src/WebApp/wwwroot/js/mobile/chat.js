(function () {
  const textarea = document.getElementById("chatInput");
  const thread = document.getElementById("chatScroll");
  const welcome = document.getElementById("chatWelcome");
  const send = document.getElementById("chatSend");
  const drawer = document.getElementById("chatDrawer");
  const backdrop = document.getElementById("drawerBackdrop");
  const panel = document.getElementById("drawerPanel");
  const historyList = document.getElementById("historyList");
  const historyCount = document.getElementById("historyCount");
  const threadPad = "pb-[calc(var(--m-nav-height)+var(--m-safe-bottom)+7rem)]";

  const replies = {
    "Lihat preferensi dan kategori": "Kategori yang bisa kamu telusuri: jenjang (S1, S2, S3), tipe dana (penuh, sebagian, bantuan UKT), dan bidang studi. Ceritakan tujuanmu, nanti aku susunkan yang paling relevan.",
    "Mulai cari rekomendasi": "Siap. Tulis jenjang, bidang, dan preferensi daerah atau luar negeri. Aku tidak menebak IPK atau profil akademikmu."
  };

  let fresh = true;
  let closeTimer = 0;

  function resize() {
    textarea.style.height = "auto";
    textarea.style.height = textarea.scrollHeight + "px";
  }

  function stamp() {
    return new Date().toLocaleTimeString("id-ID", { hour: "2-digit", minute: "2-digit" });
  }

  function scrollDown() {
    window.scrollTo({ top: document.documentElement.scrollHeight, behavior: "smooth" });
  }

  function replyFor(text) {
    return replies[text] || "Baik, saya catat pertanyaanmu. Untuk detail resmi, cek booklet dan portal penyedia beasiswa.";
  }

  function appendUser(text) {
    const row = document.createElement("div");
    row.className = "flex justify-end";
    row.innerHTML =
      '<div class="max-w-[85%] rounded-2xl rounded-tr-sm bg-forest px-4 py-3 text-white shadow-subtle">' +
      '<p class="text-xs leading-relaxed"></p>' +
      '<div class="mt-1 flex items-center justify-end gap-1 text-[10px] text-white/80"><span></span></div></div>';
    row.querySelector("p").textContent = text;
    row.querySelector("span").textContent = stamp();
    thread.appendChild(row);
  }

  function appendBot(text) {
    const block = document.createElement("div");
    block.className = "space-y-3";
    block.innerHTML =
      '<div class="flex items-center gap-2">' +
      '<div class="flex h-7 w-7 items-center justify-center rounded-lg border border-forest-border/80 bg-[#eaf4ee] text-xs font-bold text-forest shadow-sm">S</div>' +
      '<span class="text-xs font-bold text-slate-800">ScholarBot</span>' +
      '<span class="text-[10px] text-slate-400"></span></div>' +
      '<div class="rounded-2xl border border-emerald-100/90 bg-white p-4 text-xs leading-relaxed text-slate-700 shadow-subtle"></div>';
    block.querySelector(".text-slate-400").textContent = stamp();
    block.querySelector(".bg-white").textContent = text;
    thread.appendChild(block);
  }

  function showThread() {
    welcome.classList.add("hidden");
    thread.dataset.empty = "false";
    thread.classList.add("space-y-6", threadPad);
  }

  function showWelcome() {
    thread.querySelectorAll(":scope > :not(#chatWelcome)").forEach((node) => node.remove());
    welcome.classList.remove("hidden");
    thread.dataset.empty = "true";
    thread.classList.remove("space-y-6", threadPad);
    fresh = true;
    window.scrollTo({ top: 0 });
  }

  function historyButton(title) {
    const button = document.createElement("button");
    button.type = "button";
    button.dataset.historyTitle = title;
    button.className = "group flex w-full items-center gap-2.5 rounded-xl px-2.5 py-2 text-left text-xs text-slate-700 transition hover:bg-forest-soft hover:text-forest";
    button.innerHTML =
      '<svg class="h-3.5 w-3.5 shrink-0 text-slate-400 group-hover:text-forest" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"></path></svg>' +
      '<span class="truncate font-medium"></span>';
    button.querySelector("span").textContent = title;
    return button;
  }

  function ensureTodayGroup() {
    let group = historyList.querySelector('[data-history-group="Hari Ini"]');
    if (group) return group;
    const block = document.createElement("div");
    block.className = "space-y-1.5";
    block.dataset.historyBlock = "";
    block.innerHTML = '<p class="px-2 text-[10px] font-bold tracking-wider text-slate-400 uppercase">Hari Ini</p><div class="space-y-1.5" data-history-group="Hari Ini"></div>';
    historyList.prepend(block);
    return block.querySelector("[data-history-group]");
  }

  function updateCount() {
    const total = historyList.querySelectorAll("[data-history-title]").length;
    historyCount.textContent = total + " riwayat tersimpan";
  }

  function remember(title) {
    const titles = [...historyList.querySelectorAll("[data-history-title]")];
    if (titles.some((button) => button.dataset.historyTitle === title)) return;
    ensureTodayGroup().prepend(historyButton(title));
    updateCount();
  }

  function submitText(text) {
    const value = text.trim();
    if (!value) return;
    if (fresh) remember(value);
    fresh = false;
    showThread();
    appendUser(value);
    appendBot(replyFor(value));
    textarea.value = "";
    resize();
    scrollDown();
  }

  function openDrawer() {
    clearTimeout(closeTimer);
    drawer.classList.remove("pointer-events-none");
    backdrop.classList.remove("opacity-0");
    panel.classList.remove("-translate-x-full");
    document.body.classList.add("overflow-hidden");
  }

  function closeDrawer() {
    panel.classList.add("-translate-x-full");
    backdrop.classList.add("opacity-0");
    document.body.classList.remove("overflow-hidden");
    clearTimeout(closeTimer);
    closeTimer = window.setTimeout(() => {
      if (panel.classList.contains("-translate-x-full")) drawer.classList.add("pointer-events-none");
    }, 300);
  }

  function openHistory(title) {
    showWelcome();
    fresh = false;
    showThread();
    appendUser(title);
    appendBot(replyFor(title));
    closeDrawer();
    scrollDown();
  }

  textarea.addEventListener("input", resize);
  textarea.addEventListener("keydown", (event) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      submitText(textarea.value);
    }
  });
  send.addEventListener("click", () => submitText(textarea.value));

  document.getElementById("btnOpenDrawer").addEventListener("click", openDrawer);
  document.getElementById("btnCloseDrawer").addEventListener("click", closeDrawer);
  backdrop.addEventListener("click", closeDrawer);
  document.getElementById("btnNewChat").addEventListener("click", () => {
    showWelcome();
    closeDrawer();
  });
  document.getElementById("btnClearHistory").addEventListener("click", () => {
    historyList.querySelectorAll("[data-history-block]").forEach((block) => block.remove());
    updateCount();
  });

  document.querySelectorAll("[data-chat-prompt]").forEach((button) => {
    button.addEventListener("click", () => submitText(button.dataset.chatPrompt));
  });

  historyList.addEventListener("click", (event) => {
    const button = event.target.closest("[data-history-title]");
    if (button) openHistory(button.dataset.historyTitle);
  });

  document.addEventListener("keydown", (event) => {
    if (event.key === "Escape") closeDrawer();
  });
})();
