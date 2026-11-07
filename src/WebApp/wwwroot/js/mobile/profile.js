(function () {
  const form = document.getElementById("profile-form");
  if (!form) return;

  const state = {
    isEditing: false,
    currentData: {
      nama: document.getElementById("input-nama").value,
      instansi: document.getElementById("input-instansi").value,
      ipk: document.getElementById("input-ipk").value,
      deskripsi: document.getElementById("input-deskripsi").value,
      avatarSrc: null
    }
  };

  const headerNormal = document.getElementById("header-normal");
  const headerEdit = document.getElementById("header-edit");
  const modal = document.getElementById("modal-konfirmasi");
  const toast = document.getElementById("toast-notification");
  const toastMsg = document.getElementById("toast-message");
  const inputNama = document.getElementById("input-nama");
  const inputInstansi = document.getElementById("input-instansi");
  const inputIpk = document.getElementById("input-ipk");
  const inputDeskripsi = document.getElementById("input-deskripsi");
  const charCounter = document.getElementById("char-counter");
  const displayName = document.getElementById("display-name");
  const displayInstansi = document.getElementById("display-instansi");
  const profileInputs = document.querySelectorAll(".profile-input");

  let toastTimeout;

  function showToast(message) {
    if (toastTimeout) clearTimeout(toastTimeout);
    toastMsg.textContent = message;
    toast.classList.remove("-translate-y-20", "opacity-0");
    toast.classList.add("translate-y-0", "opacity-100");
    toastTimeout = setTimeout(() => {
      toast.classList.remove("translate-y-0", "opacity-100");
      toast.classList.add("-translate-y-20", "opacity-0");
    }, 3000);
  }

  function show(el) {
    el.classList.remove("hidden");
    el.classList.add("flex");
  }

  function hide(el) {
    el.classList.add("hidden");
    el.classList.remove("flex");
  }

  function enterEditMode() {
    state.isEditing = true;
    hide(headerNormal);
    show(headerEdit);
    profileInputs.forEach((input) => input.classList.add("edit-mode-active"));
    inputNama.focus();
  }

  function exitEditMode(revert) {
    state.isEditing = false;
    hide(headerEdit);
    show(headerNormal);
    profileInputs.forEach((input) => input.classList.remove("edit-mode-active"));

    if (!revert) return;

    inputNama.value = state.currentData.nama;
    inputInstansi.value = state.currentData.instansi;
    inputIpk.value = state.currentData.ipk;
    inputDeskripsi.value = state.currentData.deskripsi;
    updateCharCounter();
  }

  function updateCharCounter() {
    charCounter.textContent = inputDeskripsi.value.length + " / " + inputDeskripsi.maxLength;
  }

  inputDeskripsi.addEventListener("input", updateCharCounter);
  updateCharCounter();

  document.getElementById("btn-start-edit").addEventListener("click", enterEditMode);
  document.getElementById("btn-cancel-edit").addEventListener("click", () => exitEditMode(true));
  document.getElementById("btn-confirm-trigger").addEventListener("click", () => show(modal));
  document.getElementById("modal-btn-cancel").addEventListener("click", () => hide(modal));

  const applyButton = document.getElementById("modal-btn-apply");
  applyButton.addEventListener("click", async () => {
    if (applyButton.disabled) return;
    applyButton.disabled = true;
    const originalLabel = applyButton.textContent;
    applyButton.textContent = "Menyimpan...";
    try {
      const body = new FormData(form);
      // Form values use invariant decimal notation for ASP.NET model binding.
      body.set("Input.FinalScore", inputIpk.value.trim().replace(",", "."));
      const response = await fetch(form.action, {
        method: "POST",
        headers: { "X-Requested-With": "XMLHttpRequest" },
        body
      });
      if (response.redirected || response.status === 401) {
        throw new Error("Sesi login berakhir. Muat ulang halaman dan masuk kembali.");
      }
      const result = await response.json();
      if (!response.ok) {
        const messages = result.errors ? Object.values(result.errors).flat() : [];
        throw new Error(messages.join(" ") || result.detail || "Profil gagal disimpan.");
      }
      // Reload persisted values; do not show success for a change stored only in the DOM.
      window.location.assign(result.redirect || window.location.pathname);
    } catch (error) {
      hide(modal);
      showToast(error.message || "Profil gagal disimpan. Silakan coba lagi.");
    } finally {
      applyButton.disabled = false;
      applyButton.textContent = originalLabel;
    }
  });

  if (form.dataset.error) showToast(form.dataset.error);

  modal.addEventListener("click", (event) => {
    if (event.target === modal) hide(modal);
  });
})();
