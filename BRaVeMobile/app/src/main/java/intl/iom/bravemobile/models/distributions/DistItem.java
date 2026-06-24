package intl.iom.bravemobile.models.distributions;

public class DistItem {

    public int id;
    public String sku;
    public String externalId;
    public String name;
    public String source;
    public double quantity;
    public String uoM;

    @Override
    public String toString() {
        return name + " - " + quantity + " " + uoM;
    }


}
