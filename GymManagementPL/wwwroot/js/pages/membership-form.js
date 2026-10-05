// Membership form: live preview of price and end date for the selected plan.
(function () {
  "use strict";
  var plan = document.getElementById("PlanId");
  var start = document.getElementById("StartDate");
  var box = document.getElementById("planPreview");
  if (!plan || !box) return;

  function render() {
    var o = plan.selectedOptions[0];
    if (!o || !o.value) { box.hidden = true; return; }
    var days = parseInt(o.dataset.days, 10), price = parseFloat(o.dataset.price);
    var from = start && start.value ? new Date(start.value + "T00:00:00") : new Date();
    var to = new Date(from.getTime()); to.setDate(to.getDate() + days - 1);
    document.getElementById("previewPrice").textContent = price.toLocaleString(undefined, { maximumFractionDigits: 2 }) + " EGP";
    document.getElementById("previewDays").textContent = days + (days === 1 ? " day" : " days");
    document.getElementById("previewEnd").textContent = to.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
    box.hidden = false;
  }
  plan.addEventListener("change", render);
  if (start) start.addEventListener("change", render);
  render();
})();
