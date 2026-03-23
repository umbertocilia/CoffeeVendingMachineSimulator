const toastStack = document.getElementById("toast-stack");
const sidebarToggle = document.getElementById("sidebar-toggle");
const sidebarBackdrop = document.getElementById("sidebar-backdrop");

export function showToast(message, kind = "default") {
    const element = document.createElement("div");
    element.className = `toast ${kind}`;
    element.textContent = message;
    toastStack.prepend(element);

    setTimeout(() => {
        element.remove();
    }, 4000);
}

export function bindNavigation(onNavigate) {
    document.querySelectorAll(".nav-item").forEach(button => {
        button.addEventListener("click", () => {
            const section = button.dataset.section;
            document.querySelectorAll(".nav-item").forEach(item => item.classList.toggle("active", item === button));
            document.querySelectorAll(".view").forEach(view => view.classList.toggle("active", view.id === `view-${section}`));
            onNavigate(section);
        });
    });
}

export function bindSidebarToggle(onToggle) {
    sidebarToggle?.setAttribute("aria-expanded", String(window.innerWidth > 1120));

    sidebarToggle?.addEventListener("click", () => {
        if (window.innerWidth <= 1120) {
            const isOpen = document.body.classList.toggle("sidebar-open");
            sidebarToggle.setAttribute("aria-expanded", String(isOpen));
            onToggle?.(isOpen);
            return;
        }

        const isCollapsed = document.body.classList.toggle("sidebar-collapsed");
        sidebarToggle.setAttribute("aria-expanded", String(!isCollapsed));
        onToggle?.(!isCollapsed);
    });

    sidebarBackdrop?.addEventListener("click", () => {
        closeSidebar();
        onToggle?.(false);
    });

    window.addEventListener("resize", () => {
        if (window.innerWidth > 1120) {
            closeSidebar();
            sidebarToggle?.setAttribute("aria-expanded", String(!document.body.classList.contains("sidebar-collapsed")));
            onToggle?.(false);
            return;
        }

        sidebarToggle?.setAttribute("aria-expanded", String(document.body.classList.contains("sidebar-open")));
    });
}

export function closeSidebar() {
    if (window.innerWidth <= 1120) {
        document.body.classList.remove("sidebar-open");
        sidebarToggle?.setAttribute("aria-expanded", "false");
    }
}
