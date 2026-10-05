(function () {
  "use strict";

  /* ---- Toasts (server-rendered flash messages) ------------------------ */
  document.querySelectorAll(".toast[data-flash]").forEach(function (el) {
    new bootstrap.Toast(el, { delay: el.dataset.flash === "error" ? 8000 : 4500 }).show();
  });

  /* ---- Theme toggle ---------------------------------------------------- */
  document.querySelectorAll("[data-theme-toggle]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var next = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
      document.documentElement.setAttribute("data-bs-theme", next);
      try { localStorage.setItem("pf-theme", next); } catch (e) { }
      document.dispatchEvent(new CustomEvent("pf:theme", { detail: next }));
    });
  });

  /* ---- Confirmation modal for destructive POST forms ------------------- */
  var modalEl = document.getElementById("confirmModal");
  if (modalEl) {
    var modal = new bootstrap.Modal(modalEl);
    var pendingForm = null;
    document.addEventListener("submit", function (e) {
      var form = e.target;
      if (!(form instanceof HTMLFormElement) || !form.hasAttribute("data-confirm") || form.dataset.confirmed) return;
      e.preventDefault();
      pendingForm = form;
      modalEl.querySelector("[data-confirm-title]").textContent = form.dataset.confirmTitle || "Are you sure?";
      modalEl.querySelector("[data-confirm-body]").textContent = form.dataset.confirm;
      var ok = modalEl.querySelector("[data-confirm-ok]");
      ok.textContent = form.dataset.confirmOk || "Confirm";
      ok.className = "btn " + (form.dataset.confirmStyle || "btn-danger");
      modal.show();
    });
    modalEl.querySelector("[data-confirm-ok]").addEventListener("click", function () {
      if (!pendingForm) return;
      pendingForm.dataset.confirmed = "1";
      modal.hide();
      pendingForm.requestSubmit();
    });
  }

  /* ---- Prevent double submits ------------------------------------------ */
  document.addEventListener("submit", function (e) {
    var form = e.target;
    if (!(form instanceof HTMLFormElement) || e.defaultPrevented) return;
    // let jquery-validate stop invalid forms first
    setTimeout(function () {
      if (e.defaultPrevented) return;
      var btn = form.querySelector("button[type=submit]:not([data-no-loading])");
      if (btn) { btn.classList.add("btn-loading"); btn.setAttribute("aria-busy", "true"); }
    }, 0);
  });

  /* ---- Auto-submit filter selects -------------------------------------- */
  document.querySelectorAll("select[data-autosubmit]").forEach(function (sel) {
    sel.addEventListener("change", function () { sel.form.submit(); });
  });

  /* ---- Photo preview ---------------------------------------------------- */
  document.querySelectorAll("input[type=file][data-preview]").forEach(function (input) {
    var target = document.querySelector(input.dataset.preview);
    if (!target) return;
    input.addEventListener("change", function () {
      var file = input.files && input.files[0];
      if (!file || !file.type.startsWith("image/")) return;
      var url = URL.createObjectURL(file);
      if (target.tagName === "IMG") { target.src = url; return; }
      var img = document.createElement("img");
      img.className = target.className.replace(/av-\d/g, "");
      img.alt = "Selected photo";
      img.src = url;
      target.replaceWith(img);
      img.id = target.id;
    });
  });

  /* ---- Charts ----------------------------------------------------------- */
  var charts = [];
  function css(name) { return getComputedStyle(document.documentElement).getPropertyValue(name).trim(); }
  function buildCharts() {
    if (typeof Chart === "undefined") return;
    charts.forEach(function (c) { c.destroy(); }); charts = [];
    var text = css("--pf-muted"), grid = css("--pf-border"), brand = css("--pf-brand");
    Chart.defaults.font.family = css("--bs-font-sans-serif");
    Chart.defaults.color = text;

    document.querySelectorAll("canvas[data-chart]").forEach(function (canvas) {
      var cfg = JSON.parse(canvas.dataset.chart);
      if (cfg.type === "bar-line") {
        charts.push(new Chart(canvas, {
          data: {
            labels: cfg.labels,
            datasets: [
              { type: "bar", label: cfg.barLabel, data: cfg.bars, yAxisID: "y", backgroundColor: brand, borderRadius: 8, maxBarThickness: 34 },
              { type: "line", label: cfg.lineLabel, data: cfg.line, yAxisID: "y1", borderColor: "#f97316", backgroundColor: "#f97316", tension: .35, pointRadius: 4 }
            ]
          },
          options: {
            maintainAspectRatio: false, interaction: { mode: "index", intersect: false },
            plugins: { legend: { position: "bottom", labels: { usePointStyle: true, boxWidth: 8 } } },
            scales: {
              x: { grid: { display: false } },
              y: { beginAtZero: true, grid: { color: grid }, ticks: { precision: 0 }, title: { display: true, text: cfg.barLabel } },
              y1: { beginAtZero: true, position: "right", grid: { display: false }, title: { display: true, text: cfg.lineLabel } }
            }
          }
        }));
      } else {
        var palette = ["#4f46e5", "#0ea5e9", "#10b981", "#f97316", "#ec4899", "#8b5cf6", "#eab308"];
        charts.push(new Chart(canvas, {
          type: "doughnut",
          data: { labels: cfg.labels, datasets: [{ data: cfg.values, backgroundColor: palette, borderWidth: 0 }] },
          options: { maintainAspectRatio: false, cutout: "68%", plugins: { legend: { position: "bottom", labels: { usePointStyle: true, boxWidth: 8 } } } }
        }));
      }
    });
  }
  buildCharts();
  document.addEventListener("pf:theme", function () { setTimeout(buildCharts, 30); });
})();
