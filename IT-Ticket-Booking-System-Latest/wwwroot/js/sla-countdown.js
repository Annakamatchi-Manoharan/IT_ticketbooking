(function () {
  function pad(n) {
    return n < 10 ? "0" + n : String(n);
  }

  function formatRemaining(ms) {
    if (ms <= 0) return "Overdue";
    var s = Math.floor(ms / 1000);
    var h = Math.floor(s / 3600);
    var m = Math.floor((s % 3600) / 60);
    var sec = s % 60;
    if (h > 0) return h + "h " + pad(m) + "m";
    if (m > 0) return m + "m " + pad(sec) + "s";
    return sec + "s";
  }

  function tick() {
    var nodes = document.querySelectorAll(".sla-countdown[data-deadline]");
    var now = Date.now();
    nodes.forEach(function (el) {
      var iso = el.getAttribute("data-deadline");
      if (!iso) return;
      var end = Date.parse(iso);
      if (Number.isNaN(end)) return;
      var left = end - now;
      el.textContent = left <= 0 ? "Overdue" : "Due in " + formatRemaining(left);
      if (left <= 0) el.classList.add("text-rose-300");
    });
  }

  tick();
  setInterval(tick, 1000);
})();
