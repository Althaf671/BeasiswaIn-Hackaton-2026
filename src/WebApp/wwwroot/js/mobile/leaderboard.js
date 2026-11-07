(function () {
  const rows = Array.from(document.querySelectorAll("[data-rank-row]"));
  const search = document.getElementById("rankSearch");
  const tabs = document.getElementById("rankTabs");
  const scroll = document.getElementById("rankScroll");
  const fade = document.getElementById("rankFade");
  const empty = document.getElementById("rankEmpty");
  let limit = Infinity;

  const idle = "rounded-md border border-gray-200 bg-white px-2.5 py-1 text-[11px] font-semibold text-gray-600 shadow-sm";
  const active = "rounded-md border border-[#005f38] bg-[#005f38] px-3 py-1 text-[11px] font-bold text-white shadow-sm";

  function apply() {
    const query = (search.value || "").trim().toLowerCase();
    let visibleCount = 0;
    rows.forEach((row) => {
      const rank = Number(row.dataset.rank);
      const name = (row.dataset.name || "").toLowerCase();
      const visible = rank <= limit && name.includes(query);
      row.classList.toggle("hidden", !visible);
      if (visible) visibleCount += 1;
    });
    empty.classList.toggle("hidden", visibleCount !== 0);
    updateFade();
  }

  function updateFade() {
    const canScroll = scroll.scrollHeight - scroll.clientHeight > 4;
    const atEnd = scroll.scrollTop + scroll.clientHeight >= scroll.scrollHeight - 4;
    fade.classList.toggle("hidden", !canScroll || atEnd);
  }

  tabs.addEventListener("click", (event) => {
    const button = event.target.closest("[data-rank-tab]");
    if (!button) return;
    limit = button.dataset.rankTab === "all" ? Infinity : Number(button.dataset.rankTab);
    tabs.querySelectorAll("[data-rank-tab]").forEach((item) => {
      item.className = idle;
    });
    button.className = active;
    scroll.scrollTop = 0;
    apply();
  });

  search.addEventListener("input", apply);
  scroll.addEventListener("scroll", updateFade);
  apply();
})();
