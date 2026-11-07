(function () {
  const tabs = Array.from(document.querySelectorAll("#notifTabs [data-filter]"));
  const cards = Array.from(document.querySelectorAll("[data-notification]"));
  const unreadSection = document.getElementById("unreadSection");
  const readSection = document.getElementById("readSection");
  const unreadList = document.getElementById("unreadList");
  const readList = document.getElementById("readList");
  const unreadCount = document.getElementById("unreadCount");
  const empty = document.getElementById("notifEmpty");

  const activeTab = "bg-white font-bold text-[#00613a] shadow-sm";
  const idleTab = "font-medium text-slate-500 hover:text-slate-800";
  let filter = "all";

  function paintTabs() {
    tabs.forEach((tab) => {
      const active = tab.dataset.filter === filter;
      tab.setAttribute("aria-pressed", String(active));
      activeTab.split(" ").forEach((cls) => tab.classList.toggle(cls, active));
      idleTab.split(" ").forEach((cls) => tab.classList.toggle(cls, !active));
    });
  }

  function render() {
    cards.forEach((card) => {
      card.classList.toggle("hidden", filter !== "all" && card.dataset.status !== filter);
    });
    const visibleIn = (list) => list.querySelectorAll("[data-notification]:not(.hidden)").length;
    const unreadVisible = visibleIn(unreadList);
    const readVisible = visibleIn(readList);
    unreadSection.classList.toggle("hidden", unreadVisible === 0);
    readSection.classList.toggle("hidden", readVisible === 0);
    empty.classList.toggle("hidden", unreadVisible + readVisible > 0);
    unreadCount.textContent = unreadList.querySelectorAll("[data-notification]").length;
  }

  function markRead(card) {
    card.querySelector("[data-unread-dot]")?.remove();
    readList.prepend(card);
  }

  tabs.forEach((tab) => {
    tab.addEventListener("click", () => {
      filter = tab.dataset.filter;
      paintTabs();
      render();
    });
  });

  document.getElementById("btnMarkAll").addEventListener("click", () => {
    Array.from(unreadList.querySelectorAll("[data-notification]")).reverse().forEach(markRead);
    render();
  });

  unreadList.addEventListener("click", (event) => {
    const card = event.target.closest("[data-notification]");
    if (card && !event.target.closest("a")) {
      markRead(card);
      render();
    }
  });

  document.getElementById("btnBack").addEventListener("click", () => {
    if (window.history.length > 1) window.history.back();
    else window.location.href = "/";
  });

  paintTabs();
  render();
})();
