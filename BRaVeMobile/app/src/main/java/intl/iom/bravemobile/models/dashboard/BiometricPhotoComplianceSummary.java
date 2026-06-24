package intl.iom.bravemobile.models.dashboard;

public final class BiometricPhotoComplianceSummary {
    public final int biometricAllCollected;
    public final int biometricSomeMissing;
    public final int photoCollected;
    public final int photoMissing;

    public BiometricPhotoComplianceSummary(int biometricAllCollected,
                                           int biometricSomeMissing,
                                           int photoCollected,
                                           int photoMissing) {
        this.biometricAllCollected = biometricAllCollected;
        this.biometricSomeMissing = biometricSomeMissing;
        this.photoCollected = photoCollected;
        this.photoMissing = photoMissing;
    }
}