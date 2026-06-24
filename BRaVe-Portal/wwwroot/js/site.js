// ======================================================
// GLOBAL LOADER SYSTEM (3s delay)
// Works for forms, fetch, ajax, and page navigation
// ======================================================

let loaderTimeout = null;
let activeRequests = 0;

// ------------------------------------------------------
// Show loader
// ------------------------------------------------------
function showLoader() {

    const loader = document.getElementById("globalLoader");

    if (!loader) return;

    loader.classList.remove("d-none");
}

// ------------------------------------------------------
// Hide loader
// ------------------------------------------------------
function hideLoader() {

    const loader = document.getElementById("globalLoader");

    if (!loader) return;

    loader.classList.add("d-none");
}

// ------------------------------------------------------
// Start delayed loader
// ------------------------------------------------------



function startDelayedLoader() {

    activeRequests++;

    if (loaderTimeout) return;

    loaderTimeout = setTimeout(() => {

        if (activeRequests > 0) {
            showLoader();
        }

    }, 3000); // 3 seconds
}

// ------------------------------------------------------
// Stop loader
// ------------------------------------------------------
function stopDelayedLoader() {

    activeRequests = Math.max(0, activeRequests - 1);

    if (activeRequests > 0) return;

    if (loaderTimeout) {
        clearTimeout(loaderTimeout);
        loaderTimeout = null;
    }

    hideLoader();
}

// ======================================================
// PAGE LOAD
// ======================================================

window.addEventListener("load", function () {
    hideLoader();
});

window.addEventListener("pageshow", function (event) {
    // Fires when page is restored from bfcache (back/forward)
    activeRequests = 0;

    if (loaderTimeout) {
        clearTimeout(loaderTimeout);
        loaderTimeout = null;
    }

    hideLoader();
});


// ======================================================
// FORM SUBMISSIONS
// ======================================================

document.addEventListener("submit", function (e) {

    const form = e.target;

    if (!form || form.tagName !== "FORM") return;

    if (form.hasAttribute("data-no-loader")) return;

    startDelayedLoader();

});


// ======================================================
// PAGE NAVIGATION (links)
// ======================================================

document.addEventListener("click", function (e) {

    const link = e.target.closest("a");
    if (!link) return;

    if (link.target === "_blank") return;

    if (link.hasAttribute("data-no-loader")) return;

    const href = link.getAttribute("href");
    if (!href) return;

    // ignore download links
    if (href.includes("Download") || href.includes("download") || href.includes("attachment"))
        return;

    if (href.startsWith("#")) return;

    startDelayedLoader();

});


// ======================================================
// FETCH REQUESTS
// ======================================================

const originalFetch = window.fetch;

window.fetch = async function (...args) {

    startDelayedLoader();

    try {

        const response = await originalFetch(...args);

        return response;

    }
    finally {

        stopDelayedLoader();

    }
};


// ======================================================
// JQUERY AJAX SUPPORT
// ======================================================

if (window.jQuery) {

    $(document)
        .ajaxStart(function () {
            startDelayedLoader();
        })
        .ajaxStop(function () {
            stopDelayedLoader();
        });

}



// ======================================================
// IMAGE PREVIEW MODAL
// ======================================================

window.openImagePreview = function (name, src) {

    const nameEl = document.getElementById("previewName");
    const imgEl = document.getElementById("previewImage");
    const modalEl = document.getElementById("imagePreviewModal");

    if (!nameEl || !imgEl || !modalEl) return;

    nameEl.innerText = name || "";
    imgEl.src = src || "";

    bootstrap.Modal
        .getOrCreateInstance(modalEl)
        .show();
}
