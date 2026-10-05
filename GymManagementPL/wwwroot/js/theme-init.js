// Runs in <head> before first paint so the chosen theme never flashes.
(function () {
  try {
    var saved = localStorage.getItem("pf-theme");
    var theme = saved || (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");
    document.documentElement.setAttribute("data-bs-theme", theme);
  } catch (e) { /* storage blocked: stay on the default theme */ }
})();
