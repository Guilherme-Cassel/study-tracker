// The chosen theme lives in localStorage and is applied as data-theme on <html>.
// index.html applies it inline before Blazor starts, so the first paint already has the right colors.
const KEY = "theme";

export function get() {
    try {
        const value = localStorage.getItem(KEY);
        return value === "light" || value === "dark" ? value : "system";
    } catch {
        return "system";
    }
}

export function set(theme) {
    try {
        if (theme === "light" || theme === "dark") localStorage.setItem(KEY, theme);
        else localStorage.removeItem(KEY);
    } catch {
        // private window or blocked storage: the choice only lasts this session
    }
    apply(theme);
}

export function apply(theme) {
    const root = document.documentElement;
    if (theme === "light" || theme === "dark") root.dataset.theme = theme;
    else delete root.dataset.theme;
}
