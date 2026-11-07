(function () {
  const essay = document.getElementById("essay");
  if (essay) {
    const count = document.getElementById("essayCount");
    const chipList = document.getElementById("chipList");
    const chipCount = document.getElementById("chipCount");
    const chipInput = document.getElementById("chipInput");
    const saved = sessionStorage.getItem("profilBeasiswaDraft");

    function chips() {
      return Array.from(chipList.querySelectorAll("[data-chip]")).map((chip) => chip.dataset.label);
    }

    function refreshCount() {
      chipCount.textContent = String(chipList.querySelectorAll("[data-chip]").length);
    }

    function addChip(label) {
      const name = label.trim();
      if (!name || chips().some((item) => item.toLowerCase() === name.toLowerCase())) return;
      const chip = document.createElement("span");
      chip.dataset.chip = "";
      chip.dataset.label = name;
      chip.className = "inline-flex items-center gap-1.5 rounded-full border border-[#b8e2cb] bg-[#e6f2eb] px-3 py-1.5 text-[12px] font-semibold text-[#004e2e]";
      chip.append(document.createTextNode(name + " "));
      const remove = document.createElement("button");
      remove.type = "button";
      remove.dataset.removeChip = "";
      remove.className = "text-xs font-bold leading-none";
      remove.setAttribute("aria-label", "Hapus " + name);
      remove.textContent = "×";
      chip.append(remove);
      chipList.append(chip);
      refreshCount();
    }

    if (saved) {
      const draft = JSON.parse(saved);
      essay.value = draft.essay || "";
      if (Array.isArray(draft.categories) && draft.categories.length) {
        chipList.innerHTML = "";
        draft.categories.forEach(addChip);
      }
    }

    essay.addEventListener("input", () => {
      count.textContent = String(essay.value.length);
    });
    count.textContent = String(essay.value.length);

    chipList.addEventListener("click", (event) => {
      const button = event.target.closest("[data-remove-chip]");
      if (!button) return;
      button.closest("[data-chip]").remove();
      refreshCount();
    });

    chipInput.addEventListener("keydown", (event) => {
      if (event.key !== "Enter") return;
      event.preventDefault();
      addChip(chipInput.value);
      chipInput.value = "";
    });

    document.getElementById("saveStep").addEventListener("click", () => {
      sessionStorage.setItem("profilBeasiswaDraft", JSON.stringify({
        essay: essay.value,
        categories: chips()
      }));
      window.location.assign("/profil-beasiswa/sertifikat");
    });
    return;
  }

  const input = document.getElementById("certificateInput");
  if (!input) return;

  const row = document.querySelector("[data-file-row]");
  const error = document.getElementById("certificateError");
  const fileCount = document.getElementById("fileCount");
  const maxBytes = 5 * 1024 * 1024;

  function setCount(value) {
    fileCount.textContent = String(value);
  }

  document.getElementById("removeFile").addEventListener("click", () => {
    row.classList.add("hidden");
    input.value = "";
    setCount(0);
  });

  input.addEventListener("change", () => {
    const file = input.files && input.files[0];
    error.classList.add("hidden");
    if (!file) return;
    const allowed = ["image/jpeg", "image/png", "application/pdf"];
    if (!allowed.includes(file.type) || file.size > maxBytes) {
      error.textContent = "Hanya PDF, JPG, atau PNG, maksimal 5 MB.";
      error.classList.remove("hidden");
      input.value = "";
      return;
    }
    row.querySelector("[data-file-name]").textContent = file.name;
    row.querySelector("[data-file-meta]").textContent =
      (file.size / (1024 * 1024)).toFixed(1) + " MB • Berhasil diunggah";
    row.classList.remove("hidden");
    setCount(1);
  });
})();
