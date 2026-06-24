package intl.iom.bravemobile.models.distributions;

import androidx.annotation.NonNull;

import java.util.List;

import intl.iom.bravemobile.statics.ActivityMode;
import intl.iom.bravemobile.statics.DistributionType;

public class Distribution {

    public String activityCode = "";

    public int distributionId; //from api
    public String externalId; //from api
    public String title; //from api
    public String description; //from api

    public int type; //1- Family, 2- Individual, 3- Group
    public int mode; //1- online, 2- offline, 3- hybrid
    public boolean biometricReceiptRequired = true;
    public boolean photoReceiptRequired = true;
    public String additionalInfo;
    public List<Kit> kits;

    public DistributionStaging getStaging()
    {
        return new DistributionStaging(distributionId, externalId,title,description,kits);
    }

    @NonNull
    @Override
    public String toString() {
            return String.format("type: %s • mode: %s", DistributionType.fromCode(type),ActivityMode.fromCode(mode));
    }
}
