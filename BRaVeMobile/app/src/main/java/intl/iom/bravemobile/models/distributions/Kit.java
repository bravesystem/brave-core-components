package intl.iom.bravemobile.models.distributions;

import java.util.List;

public class Kit {

    public String kitId;
    public String sku;
    public String externalId;
    public String title;
    public String description;
    public String source = "I";
    public String targetType = "K";
    public int quantity;
    public List<DistItem> items;

}
