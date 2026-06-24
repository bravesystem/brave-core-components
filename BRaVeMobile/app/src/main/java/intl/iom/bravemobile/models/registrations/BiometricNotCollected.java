package intl.iom.bravemobile.models.registrations;

public class BiometricNotCollected {
    public int selectedReason = 1;
    public String reasonIfOther;

    public BiometricNotCollected(){
        selectedReason = 1;
    }
    public BiometricNotCollected(int selectedReason, String reasonIfOther)
    {
        this.selectedReason = selectedReason;
        this.reasonIfOther = reasonIfOther;
    }
}
