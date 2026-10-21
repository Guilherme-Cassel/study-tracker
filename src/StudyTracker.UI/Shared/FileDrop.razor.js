// Files dropped anywhere else on the page must not make the browser open them.
let guarded = false;
function guardPage() {
    if (guarded) return;
    guarded = true;
    const isFileInput = e => e.target instanceof HTMLInputElement && e.target.type === "file";
    window.addEventListener("dragover", e => { if (!isFileInput(e)) e.preventDefault(); });
    window.addEventListener("drop", e => { if (!isFileInput(e)) e.preventDefault(); });
}

// Shows the drop overlay (and lets the hidden file input receive the drop) only while files are dragged over the host.
export function attach(host) {
    guardPage();
    let depth = 0;
    const hasFiles = e => Array.from(e.dataTransfer?.types ?? []).includes("Files");
    const enter = e => {
        if (!hasFiles(e)) return;
        depth++;
        host.classList.add("dragging");
    };
    const leave = e => {
        if (!hasFiles(e)) return;
        depth = Math.max(0, depth - 1);
        if (depth === 0) host.classList.remove("dragging");
    };
    const reset = () => {
        depth = 0;
        host.classList.remove("dragging");
    };

    host.addEventListener("dragenter", enter);
    host.addEventListener("dragleave", leave);
    host.addEventListener("drop", reset);
    window.addEventListener("dragend", reset);

    return {
        dispose() {
            host.removeEventListener("dragenter", enter);
            host.removeEventListener("dragleave", leave);
            host.removeEventListener("drop", reset);
            window.removeEventListener("dragend", reset);
        }
    };
}
