// Session form: default the end time, and only offer trainers who can lead the chosen category.
(function () {
  "use strict";
  var start = document.getElementById("StartDate");
  var end = document.getElementById("EndDate");
  var category = document.getElementById("CategoryId");
  var trainer = document.getElementById("TrainerId");

  function pad(n) { return String(n).padStart(2, "0"); }
  function toLocalInput(d) {
    return d.getFullYear() + "-" + pad(d.getMonth() + 1) + "-" + pad(d.getDate()) + "T" + pad(d.getHours()) + ":" + pad(d.getMinutes());
  }

  if (start && end) {
    start.addEventListener("change", function () {
      if (!start.value) return;
      var s = new Date(start.value);
      if (!end.value || new Date(end.value) <= s) end.value = toLocalInput(new Date(s.getTime() + 60 * 60000));
    });
  }

  if (trainer) {
    var options = Array.prototype.slice.call(trainer.options);
    function filterTrainers() {
      var cat = category ? category.value : "";
      var current = trainer.value;
      options.forEach(function (o) {
        if (!o.value) return;
        var allowed = !cat || (o.dataset.categories || "").split(",").indexOf(cat) !== -1;
        o.hidden = !allowed; o.disabled = !allowed;
      });
      var selected = trainer.selectedOptions[0];
      if (selected && selected.disabled) trainer.value = "";
      var hint = document.getElementById("trainerHint");
      if (hint) hint.hidden = !cat;
    }
    if (category) category.addEventListener("change", filterTrainers);
    filterTrainers();
  }
})();
