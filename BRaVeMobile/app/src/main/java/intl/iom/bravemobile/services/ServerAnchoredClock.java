package intl.iom.bravemobile.services;

import intl.iom.bravemobile.interfaces.ClockService;

public class ServerAnchoredClock implements ClockService {

    private final long serverNowUtcMs;
    private final long anchorElapsedMs = android.os.SystemClock.elapsedRealtime();
    public ServerAnchoredClock(long serverNowUtcMs) { this.serverNowUtcMs = serverNowUtcMs; }

    @Override
    public long nowMs() {
        long delta = android.os.SystemClock.elapsedRealtime() - anchorElapsedMs;
        return serverNowUtcMs + delta;
    }
}
