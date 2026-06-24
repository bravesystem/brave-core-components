package intl.iom.bravemobile.statics;

public enum ActivityMode {
    //1- ONLINE, 2- OFFLINE, 3- HYBRID
    ONLINE(1, "ONLINE"),
    OFFLINE(2, "OFFLINE"),
    HYBRID(3, "HYBRID");

    private final int id;
    private final String label;

    ActivityMode(int id, String label) {
        this.id = id;
        this.label = label;
    }

    public int getId() { return id; }
    public String getLabel() { return label; }

    public static ActivityMode fromCode(int code) {
        for (ActivityMode t : values()) {
            if (t.id == code) return t;
        }
        throw new IllegalArgumentException("Unknown ActivityMode code: " + code);
    }
}
