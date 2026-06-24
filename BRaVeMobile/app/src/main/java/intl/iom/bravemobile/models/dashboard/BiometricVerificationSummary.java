package intl.iom.bravemobile.models.dashboard;

public final class BiometricVerificationSummary {
    public final int createCount;
    public final int processingCount;
    public final int noMatchCount;
    public final int matchCount;

    public BiometricVerificationSummary(int createCount, int processingCount,
                                        int noMatchCount, int matchCount) {
        this.createCount = createCount;
        this.processingCount = processingCount;
        this.noMatchCount = noMatchCount;
        this.matchCount = matchCount;
    }
}