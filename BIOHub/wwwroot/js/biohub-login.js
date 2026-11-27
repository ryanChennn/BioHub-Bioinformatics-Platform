document.addEventListener("DOMContentLoaded", () => {
    const passwordInput = document.getElementById("Password");
    const togglePasswordButton = document.getElementById("toggle-password");
    const passwordIcon = document.getElementById("pwd-icon");

    if (passwordInput && togglePasswordButton && passwordIcon) {
        togglePasswordButton.addEventListener("click", () => {
            const showingPassword = passwordInput.type === "text";

            passwordInput.type = showingPassword ? "password" : "text";
            passwordIcon.textContent = showingPassword ? "visibility" : "visibility_off";
        });
    }

    const form = document.getElementById("biohub-login-form");
    const submitButton = document.getElementById("btn-submit");
    const submitText = document.getElementById("btn-submit-text");
    const submitIcon = document.getElementById("btn-submit-icon");

    if (form && submitButton && submitText && submitIcon) {
        form.addEventListener("submit", () => {
            if (!form.checkValidity()) {
                return;
            }

            submitButton.disabled = true;
            submitText.textContent = "Signing In...";
            submitIcon.textContent = "progress_activity";
            submitIcon.classList.add("animate-spin");
        });
    }
});
