package intl.iom.bravemobile.statics;

public enum DataPointType {
    HOUSEHOLD(1, "HOUSEHOLD"),
    INDIVIDUAL(2, "INDIVIDUAL");

    private final int code;
    private final String label;

    DataPointType(int code, String label) {
        this.code = code;
        this.label = label;
    }

    public int getCode() { return code; }
    public String getLabel() { return label; }

    /** Look up by numeric code (1..13). */
    public static DataPointType fromCode(int code) {
        for (DataPointType t : values()) {
            if (t.code == code) return t;
        }
        throw new IllegalArgumentException("Unknown Datapoint Type code: " + code);
    }

    /** Flexible lookup by enum name or human label (case-insensitive, spaces/underscores ignored). */
    public static DataPointType fromString(String s) {
        if (s == null) throw new IllegalArgumentException("null");
        String key = s.trim().toUpperCase().replace(' ', '_');
        // try enum name first
        for (DataPointType t : values()) {
            if (t.name().equals(key)) return t;
        }
        // then try label normalized
        for (DataPointType t : values()) {
            if (t.label.toUpperCase().replace(' ', '_').equals(key)) return t;
        }
        throw new IllegalArgumentException("Unknown DataPointType: " + s);
    }
}
