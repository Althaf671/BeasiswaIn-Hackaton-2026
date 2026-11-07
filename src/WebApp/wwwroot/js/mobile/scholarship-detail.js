(function () {
  function toggleAccordion(targetId, button) {
    const target = document.getElementById(targetId);
    if (!target) return;
    const label = button.querySelector("[data-accordion-label]");
    const icon = button.querySelector(".material-symbols-outlined");
    const expanded = target.classList.toggle("is-expanded");
    if (label) label.textContent = expanded ? "Read less" : "Read more";
    icon?.classList.toggle("rotate-180", expanded);
  }

  document.querySelectorAll("[data-accordion]").forEach((button) => {
    button.addEventListener("click", () => toggleAccordion(button.dataset.accordion, button));
  });

  const bar = document.querySelector(".m-detail-bar");
  const main = document.getElementById("detailMain");
  function fitDetailBar() {
    if (!bar || !main) return;
    main.style.paddingBottom = Math.ceil(bar.getBoundingClientRect().height) + "px";
  }
  fitDetailBar();
  window.addEventListener("resize", fitDetailBar);
  if (bar && typeof ResizeObserver !== "undefined") new ResizeObserver(fitDetailBar).observe(bar);

  document.getElementById("btnBack")?.addEventListener("click", () => {
    if (window.history.length > 1) window.history.back();
    else window.location.href = "/";
  });

  document.getElementById("btnShare")?.addEventListener("click", async () => {
    const payload = { title: document.title, url: window.location.href };
    if (navigator.share) {
      try {
        await navigator.share(payload);
      } catch {
        /* dismissed */
      }
      return;
    }
    try {
      await navigator.clipboard.writeText(window.location.href);
    } catch {
      /* clipboard unavailable */
    }
  });
})();
