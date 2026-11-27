// BioHub public-site JavaScript.
// Keep this file for future interactions that are not handled by Tailwind/HTML alone.

document.addEventListener("DOMContentLoaded", () => {
    // Close <details> mobile navigation after a menu link is selected.
    document.querySelectorAll("details a").forEach(link => {
        link.addEventListener("click", () => {
            const details = link.closest("details");
            if (details) details.removeAttribute("open");
        });
    });
});
