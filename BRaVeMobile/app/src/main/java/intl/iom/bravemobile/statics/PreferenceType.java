package intl.iom.bravemobile.statics;

public enum PreferenceType {

    INT(1, "INT"),
    NUMERIC(2, "NUMERIC"),
    BOOLEAN(3, "BOOLEAN"),
    TEXT(4, "TEXT");

    private final int code;
    private final String label;

    PreferenceType(int code, String label) {
        this.code = code;
        this.label = label;
    }

    public int getCode() { return code; }
    public String getLabel() { return label; }

    /** Look up by numeric code (1..13). */
    public static PreferenceType fromCode(int code) {
        for (PreferenceType t : values()) {
            if (t.code == code) return t;
        }
        throw new IllegalArgumentException("Unknown PreferenceType code: " + code);
    }

    /** Flexible lookup by enum name or human label (case-insensitive, spaces/underscores ignored). */
    public static PreferenceType fromString(String s) {
        if (s == null) throw new IllegalArgumentException("null");
        String key = s.trim().toUpperCase().replace(' ', '_');
        // try enum name first
        for (PreferenceType t : values()) {
            if (t.name().equals(key)) return t;
        }
        // then try label normalized
        for (PreferenceType t : values()) {
            if (t.label.toUpperCase().replace(' ', '_').equals(key)) return t;
        }
        throw new IllegalArgumentException("Unknown PreferenceType: " + s);
    }
}


