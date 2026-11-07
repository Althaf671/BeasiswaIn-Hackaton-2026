(function () {
  const search = document.getElementById("scholarshipSearch");
  const cards = Array.from(document.querySelectorAll("[data-scholarship-card]"));
  const empty = document.getElementById("scholarshipEmpty");
  const addModal = document.getElementById("input-modal");
  const successModal = document.getElementById("success-modal");
  const filterModal = document.getElementById("filter-modal");

  function show(el) {
    el.classList.remove("hidden");
    el.classList.add("flex");
    document.body.classList.add("overflow-hidden");
  }

  function hide(el) {
    el.classList.add("hidden");
    el.classList.remove("flex");
    document.body.classList.remove("overflow-hidden");
  }

  function applySearch() {
    const query = (search.value || "").trim().toLowerCase();
    let visible = 0;
    cards.forEach((card) => {
      const match = card.dataset.name.toLowerCase().includes(query);
      card.classList.toggle("hidden", !match);
      if (match) visible += 1;
    });
    empty.classList.toggle("hidden", visible !== 0);
  }

  search.addEventListener("input", applySearch);

  document.getElementById("openAdd").addEventListener("click", () => show(addModal));
  document.getElementById("cancelAdd").addEventListener("click", () => hide(addModal));
  document.getElementById("submitAdd").addEventListener("click", () => {
    hide(addModal);
    show(successModal);
  });
  document.getElementById("closeSuccess").addEventListener("click", () => hide(successModal));

  document.getElementById("file-upload-input").addEventListener("change", (event) => {
    const file = event.target.files && event.target.files[0];
    if (file) document.getElementById("file-label").textContent = file.name;
  });

  document.getElementById("openFilter").addEventListener("click", () => show(filterModal));
  document.getElementById("closeFilter").addEventListener("click", () => hide(filterModal));
  document.getElementById("filterBackdrop").addEventListener("click", () => hide(filterModal));
  document.getElementById("applyFilter").addEventListener("click", () => hide(filterModal));

  const idleDeadline = "rounded-lg border border-stone-200 px-1 py-2 text-[11px] font-medium text-stone-600 transition hover:border-[#00613a]";
  const activeDeadline = "rounded-lg border border-[#00613a] bg-[#00613a] px-1 py-2 text-[11px] font-medium text-white shadow-sm";

  document.getElementById("deadlineChips").addEventListener("click", (event) => {
    const button = event.target.closest("[data-deadline]");
    if (!button) return;
    document.querySelectorAll("[data-deadline]").forEach((item) => {
      item.className = idleDeadline;
    });
    button.className = activeDeadline;
  });

  document.getElementById("resetFilter").addEventListener("click", () => {
    filterModal.querySelectorAll('input[type="checkbox"]').forEach((box) => {
      box.checked = false;
    });
    document.querySelectorAll("[data-deadline]").forEach((item) => {
      item.className = idleDeadline;
    });
  });
})();
