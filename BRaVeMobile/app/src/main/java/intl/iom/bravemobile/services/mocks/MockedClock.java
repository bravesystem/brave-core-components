package intl.iom.bravemobile.services.mocks;

import intl.iom.bravemobile.interfaces.ClockService;

public class MockedClock implements ClockService {
    private long now;
    public MockedClock(long start){ now = start; }
    @Override
    public long nowMs(){ return now; }
    public void advanceMs(long d){ now += d; }
}
