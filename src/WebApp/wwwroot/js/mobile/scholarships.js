(function () {
  const search = document.getElementById("scholarshipSearch");
  const cards = Array.from(document.querySelectorAll("[data-scholarship-card]"));
  const empty = document.getElementById("scholarshipEmpty");
  const addModal = document.getElementById("input-modal");
  const successModal = document.getElementById("success-modal");
  const form = document.getElementById("submission-form");
  const errors = document.getElementById("submission-errors");
  const submitButton = document.getElementById("submitAdd");
  const cancelButton = document.getElementById("cancelAdd");
  const urlInput = document.getElementById("input-beasiswa-url");
  const fileInput = document.getElementById("file-upload-input");
  const fileLabel = document.getElementById("file-label");
  const defaultFileLabel = fileLabel.textContent;
  let submitting = false;
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
  cancelButton.addEventListener("click", () => {
    if (!submitting) hide(addModal);
  });
  form.addEventListener("submit", async (event) => {
    event.preventDefault();
    if (submitting) return;
    errors.textContent = "";
    const hasUrl = urlInput.value.trim().length > 0;
    const file = fileInput.files[0];
    if (hasUrl === Boolean(file)) {
      errors.textContent = "Pilih salah satu: tautan beasiswa atau gambar brosur.";
      return;
    }
    if (file && (file.size === 0 || file.size > 10 * 1024 * 1024)) {
      errors.textContent = "Gambar harus berisi data dan maksimal berukuran 10MB.";
      return;
    }
    submitting = true;
    submitButton.disabled = true;
    cancelButton.disabled = true;
    submitButton.textContent = "Mengirim...";
    try {
      const response = await fetch(form.action, {
        method: "POST",
        headers: { "X-Requested-With": "XMLHttpRequest" },
        body: new FormData(form)
      });
      if (response.redirected || response.status === 401 || response.status === 403) {
        throw new Error("Sesi login berakhir. Muat ulang halaman dan masuk kembali.");
      }
      if (response.status === 429) {
        throw new Error("Terlalu banyak permintaan. Tunggu sebentar lalu coba lagi.");
      }
      const result = await response.json();
      if (!response.ok || result.success !== true) {
        const messages = result.errors ? Object.values(result.errors).flat() : [];
        throw new Error(messages.join(" ") || "Informasi beasiswa gagal dikirim.");
      }
      form.reset();
      fileLabel.textContent = defaultFileLabel;
      hide(addModal);
      show(successModal);
    } catch (error) {
      errors.textContent = error.message || "Informasi beasiswa gagal dikirim. Silakan coba lagi.";
    } finally {
      submitting = false;
      submitButton.disabled = false;
      cancelButton.disabled = false;
      submitButton.textContent = "Kirimkan";
    }
  });
  if (addModal.dataset.open === "true") show(addModal);
  if (successModal.dataset.open === "true") show(successModal);
  document.getElementById("closeSuccess").addEventListener("click", () => hide(successModal));

  fileInput.addEventListener("change", () => {
    const file = fileInput.files[0];
    fileLabel.textContent = file ? file.name : defaultFileLabel;
    if (file) urlInput.value = "";
  });
  urlInput.addEventListener("input", () => {
    if (urlInput.value.trim()) {
      fileInput.value = "";
      fileLabel.textContent = defaultFileLabel;
    }
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
