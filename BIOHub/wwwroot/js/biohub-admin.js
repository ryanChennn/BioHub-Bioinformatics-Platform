document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("admin-sidebar");
    const overlay = document.getElementById("admin-sidebar-overlay");
    const openButton = document.getElementById("admin-menu-toggle");
    const closeButton = document.getElementById("admin-sidebar-close");

    const openSidebar = () => {
        if (!sidebar || !overlay) return;
        sidebar.classList.remove("-translate-x-full");
        overlay.classList.remove("hidden");
        document.body.classList.add("admin-sidebar-open");
    };

    const closeSidebar = () => {
        if (!sidebar || !overlay) return;
        sidebar.classList.add("-translate-x-full");
        overlay.classList.add("hidden");
        document.body.classList.remove("admin-sidebar-open");
    };

    openButton?.addEventListener("click", openSidebar);
    closeButton?.addEventListener("click", closeSidebar);
    overlay?.addEventListener("click", closeSidebar);

    sidebar?.querySelectorAll("a").forEach(link => {
        link.addEventListener("click", () => {
            if (window.innerWidth < 768) {
                closeSidebar();
            }
        });
    });

    window.addEventListener("resize", () => {
        if (window.innerWidth >= 768) {
            overlay?.classList.add("hidden");
            document.body.classList.remove("admin-sidebar-open");
            sidebar?.classList.remove("-translate-x-full");
        } else if (!document.body.classList.contains("admin-sidebar-open")) {
            sidebar?.classList.add("-translate-x-full");
        }
    });
});
