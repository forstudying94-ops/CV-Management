const themeStorageKey = "cv-forge-theme";

function getSavedTheme() {
    return localStorage.getItem(themeStorageKey) ?? "light";
}

function applyTheme(theme) {
    const page = document.documentElement;
    const icon = document.getElementById("theme-icon");

    page.setAttribute("data-bs-theme", theme);

    if (icon !== null) {
        icon.textContent = theme === "dark" ? "☀" : "☾";
    }
}

function switchTheme() {
    const currentTheme = document.documentElement.getAttribute("data-bs-theme");
    const nextTheme = currentTheme === "dark" ? "light" : "dark";

    localStorage.setItem(themeStorageKey, nextTheme);
    applyTheme(nextTheme);
    saveThemeForCurrentUser(nextTheme);
}

async function saveThemeForCurrentUser(theme) {
    if (!document.querySelector('input[name="__RequestVerificationToken"]')) {
        return;
    }

    try {
        const user = await window.cvApp.request("/api/auth/me");
        const result = await window.cvApp.request("/api/profile/preferences", {
            method: "PUT",
            body: {
                theme: theme === "dark" ? 1 : 0,
                version: user.version
            }
        });
        const profileVersionInput = document.querySelector('input[name="UserVersion"]');
        if (profileVersionInput) {
            profileVersionInput.value = result.version;
        }
    } catch {}
}

async function initializeTheme() {
    const themeButton = document.getElementById("theme-toggle");

    applyTheme(getSavedTheme());

    if (themeButton !== null) {
        themeButton.addEventListener("click", switchTheme);
    }

    if (localStorage.getItem(themeStorageKey) === null &&
        document.querySelector('input[name="__RequestVerificationToken"]')) {
        try {
            const user = await window.cvApp.request("/api/auth/me");
            const theme = user.theme === 1 ? "dark" : "light";
            localStorage.setItem(themeStorageKey, theme);
            applyTheme(theme);
        } catch {}
    }
}

document.addEventListener("DOMContentLoaded", initializeTheme);

window.cvApp = {
    getRequestToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value ?? "";
    },

    async request(url, options = {}) {
        const method = options.method ?? "GET";
        const headers = {
            "Accept": "application/json",
            ...(options.headers ?? {})
        };
        let body = options.body ?? null;

        if (method !== "GET" && method !== "HEAD") {
            headers.RequestVerificationToken = this.getRequestToken();
        }

        if (body !== null && !(body instanceof FormData)) {
            headers["Content-Type"] = "application/json";
            body = JSON.stringify(body);
        }

        const response = await fetch(url, {
            method,
            credentials: "same-origin",
            headers,
            body
        });

        if (!response.ok) {
            const errorBody = await response.json().catch(() => ({}));
            const validationMessage = errorBody.errors
                ? Object.values(errorBody.errors).flat()[0]
                : null;
            const error = new Error(
                errorBody.message ??
                validationMessage ??
                errorBody.title ??
                "The operation failed."
            );
            error.status = response.status;
            error.conflict = errorBody.conflict === true;
            error.details = errorBody;
            throw error;
        }

        if (response.status === 204) {
            return null;
        }

        return response.json().catch(() => null);
    },

    splitList(value) {
        return value
            .split(",")
            .map(item => item.trim())
            .filter(item => item.length > 0);
    },

    showStatus(element, message, isError = false) {
        element.textContent = message;
        element.className = isError
            ? "small mt-2 text-danger"
            : "small mt-2 text-success";
    }
};
