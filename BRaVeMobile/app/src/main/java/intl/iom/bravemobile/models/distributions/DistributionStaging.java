package intl.iom.bravemobile.models.distributions;

import java.util.List;

public class DistributionStaging {

    public int distributionId;
    public String externalId;
    public String title;
    public String description;

    public List<Kit> kits;

    public DistributionStaging(int distributionId,String externalId, String title, String description, List<Kit> kits) {
        this.distributionId = distributionId;
        this.externalId = externalId;
        this.title = title;
        this.description = description;
        this.kits = kits;
    }
}
