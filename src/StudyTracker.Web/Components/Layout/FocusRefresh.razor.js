export function watch(dotnet) {
    let last = 0;
    const onVisible = () => {
        if (document.visibilityState !== "visible") return;
        const now = Date.now();
        if (now - last < 2000) return;
        last = now;
        dotnet.invokeMethodAsync("OnVisible");
    };

    document.addEventListener("visibilitychange", onVisible);
    window.addEventListener("focus", onVisible);

    return {
        dispose() {
            document.removeEventListener("visibilitychange", onVisible);
            window.removeEventListener("focus", onVisible);
        }
    };
}
