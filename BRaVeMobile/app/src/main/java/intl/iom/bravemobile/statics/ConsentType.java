package intl.iom.bravemobile.statics;

public enum ConsentType {

    HOUSEHOLD(1, "HOUSEHOLD"),
    INDIVIDUAL(2, "INDIVIDUAL"),
    SURVEY(3, "SURVEY"),
    DISTRIBUTION(4, "DISTRIBUTION");

    private final int id;
    private final String label;

    ConsentType(int id, String label) {
        this.id = id;
        this.label = label;
    }

    public int getId() { return id; }
    public String getLabel() { return label; }

    /** Look up by numeric code (1..13). */
    public static ConsentType fromCode(int id) {
        for (ConsentType t : values()) {
            if (t.id == id) return t;
        }
        throw new IllegalArgumentException("Unknown ConsentType code: " + id);
    }

    /** Flexible lookup by enum name or human label (case-insensitive, spaces/underscores ignored). */
    public static ConsentType fromString(String s) {
        if (s == null) throw new IllegalArgumentException("null");
        String key = s.trim().toUpperCase().replace(' ', '_');
        // try enum name first
        for (ConsentType t : values()) {
            if (t.name().equals(key)) return t;
        }
        // then try label normalized
        for (ConsentType t : values()) {
            if (t.label.toUpperCase().replace(' ', '_').equals(key)) return t;
        }
        throw new IllegalArgumentException("Unknown ConsentType: " + s);
    }
}
