document.addEventListener("click", function (e) {
  var open = e.target.closest("[data-open-sidebar]");
  var close = e.target.closest("[data-close-sidebar]");
  var sidebar = document.getElementById("app-sidebar");
  var overlay = document.getElementById("sidebar-overlay");
  if (!sidebar || !overlay) return;

  if (open) {
    sidebar.classList.remove("-translate-x-full");
    overlay.classList.remove("hidden");
  }
  if (close) {
    sidebar.classList.add("-translate-x-full");
    overlay.classList.add("hidden");
  }
});
