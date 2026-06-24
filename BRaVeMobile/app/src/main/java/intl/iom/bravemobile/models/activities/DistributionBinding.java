package intl.iom.bravemobile.models.activities;

import com.google.gson.annotations.SerializedName;

public class DistributionBinding {
    public int distributionId ;

    public int type ;

    //1- ONLINE, 2- OFFLINE, 3- HYBRID
    public int mode = 1;
    @SerializedName("biometricConfirmation")
    public boolean biometricReceiptRequired = true;
    @SerializedName("photoConfirmation")
    public boolean photoReceiptRequired = true;


}
