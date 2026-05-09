(function () {
  function refreshBadge() {
    fetch("/Notifications/UnreadCount")
      .then(function (r) {
        return r.json();
      })
      .then(function (d) {
        var b = document.getElementById("notif-badge");
        if (!b || typeof d.count !== "number") return;
        if (d.count > 0) {
          b.textContent = d.count > 99 ? "99+" : String(d.count);
          b.classList.remove("hidden");
        } else {
          b.classList.add("hidden");
        }
      })
      .catch(function () {});
  }

  function loadPanel() {
    var list = document.getElementById("notif-list");
    if (!list) return;
    list.innerHTML = '<div class="px-4 py-6 text-sm text-slate-500 text-center">Loading…</div>';
    fetch("/Notifications/Recent")
      .then(function (r) {
        return r.text();
      })
      .then(function (html) {
        list.innerHTML = html;
        bindMarkReadButtons();
      })
      .catch(function () {
        list.innerHTML = '<div class="px-4 py-6 text-sm text-rose-300 text-center">Failed to load.</div>';
      });
  }

  function bindMarkReadButtons() {
    var buttons = document.querySelectorAll(".notif-markread");
    buttons.forEach(function (btn) {
      if (btn.dataset.bound === "1") return;
      btn.dataset.bound = "1";
      btn.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        var id = btn.getAttribute("data-id");
        if (!id) return;
        fetch("/Notifications/MarkRead?id=" + encodeURIComponent(id), { method: "POST" })
          .then(function () {
            refreshBadge();
            loadPanel();
          })
          .catch(function () {});
      });
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    refreshBadge();
    setInterval(refreshBadge, 60000);

    var btn = document.getElementById("notif-btn");
    var panel = document.getElementById("notif-panel");
    var markAll = document.getElementById("notif-markall");

    if (btn && panel) {
      panel.addEventListener("click", function (e) {
        e.stopPropagation();
      });
      btn.addEventListener("click", function (e) {
        e.stopPropagation();
        var hidden = panel.classList.contains("hidden");
        panel.classList.toggle("hidden", !hidden);
        if (!hidden) return;
        loadPanel();
      });
      document.addEventListener("click", function () {
        panel.classList.add("hidden");
      });
    }

    if (markAll) {
      markAll.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();
        fetch("/Notifications/MarkAllRead", { method: "POST" })
          .then(function () {
            refreshBadge();
            loadPanel();
          })
          .catch(function () {});
      });
    }

    try {
      var conn = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/tickets")
        .withAutomaticReconnect()
        .build();

      conn.on("TicketCreated", function () {
        refreshBadge();
      });
      conn.on("TicketUpdated", function () {
        refreshBadge();
        if (window.Swal && typeof window.Swal.fire === "function") {
          window.Swal.fire({
            toast: true,
            position: "top-end",
            icon: "info",
            timer: 2500,
            showConfirmButton: false,
            title: "Ticket activity updated"
          });
        }
      });

      conn.start().catch(function () {});
    } catch (e) {}
  });
})();
