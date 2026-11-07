(function () {
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
    charCounter.textContent = inputDeskripsi.value.length + " / 500";
  }

  inputDeskripsi.addEventListener("input", updateCharCounter);
  updateCharCounter();

  document.getElementById("btn-start-edit").addEventListener("click", enterEditMode);
  document.getElementById("btn-cancel-edit").addEventListener("click", () => exitEditMode(true));
  document.getElementById("btn-confirm-trigger").addEventListener("click", () => show(modal));
  document.getElementById("modal-btn-cancel").addEventListener("click", () => hide(modal));

  document.getElementById("modal-btn-apply").addEventListener("click", () => {
    state.currentData.nama = inputNama.value.trim() || state.currentData.nama;
    state.currentData.instansi = inputInstansi.value.trim() || state.currentData.instansi;
    state.currentData.ipk = inputIpk.value.trim() || state.currentData.ipk;
    state.currentData.deskripsi = inputDeskripsi.value.trim();
    inputNama.value = state.currentData.nama;
    inputInstansi.value = state.currentData.instansi;
    inputIpk.value = state.currentData.ipk;
    displayName.textContent = state.currentData.nama;
    displayInstansi.textContent = state.currentData.instansi;
    hide(modal);
    exitEditMode(false);
    showToast("Perubahan profil berhasil diterapkan!");
  });

  modal.addEventListener("click", (event) => {
    if (event.target === modal) hide(modal);
  });
})();
