(function () {
    const SESSION_URL = '/api/auth/session';
    const LOGOUT_URL = '/logout';
    const CHECK_INTERVAL_MS = 60 * 1000;

    let redirecting = false;

    function redirectToLogout() {
        if (redirecting) return;
        redirecting = true;
        window.location.href = LOGOUT_URL;
    }

    async function checkSession() {
        try {
            const response = await fetch(SESSION_URL, {
                method: 'GET',
                credentials: 'same-origin',
                cache: 'no-store',
                headers: { Accept: 'application/json' }
            });

            if (response.status === 401) {
                redirectToLogout();
            }
        } catch {
            // Ignore transient network errors.
        }
    }

    document.addEventListener('visibilitychange', function () {
        if (document.visibilityState === 'visible') {
            checkSession();
        }
    });

    window.setInterval(checkSession, CHECK_INTERVAL_MS);
    checkSession();
})();
