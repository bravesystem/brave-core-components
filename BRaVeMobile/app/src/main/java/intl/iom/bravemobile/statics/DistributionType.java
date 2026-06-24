package intl.iom.bravemobile.statics;

public enum DistributionType {
    //1- Family, 2- Individual, 3- Group
    FAMILY(1, "FAMILY"),
    INDIVIDUAL(2, "INDIVIDUAL"),
    BOTH(3, "BOTH");

    private final int id;
    private final String label;

    DistributionType(int id, String label) {
        this.id = id;
        this.label = label;
    }

    public int getId() { return id; }
    public String getLabel() { return label; }

    public static DistributionType fromCode(int code) {
        for (DistributionType t : values()) {
            if (t.id == code) return t;
        }
        throw new IllegalArgumentException("Unknown DistributionType code: " + code);
    }
}
