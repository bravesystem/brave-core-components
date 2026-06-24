package intl.iom.bravemobile.statics;

public enum AnswerType {
    INT(1, "INT"),
    NUMERIC(2, "NUMERIC"),
    TEXT(3, "TEXT"),
    DATE(4, "DATE"),
    BOOLEAN(5, "BOOLEAN"),
    SELECT_ONE(6, "SELECT ONE"),
    SELECT_MULTIPLE(7, "SELECT MULTIPLE"),
    GPS_COORDINATES(8, "GPS COORDINATES"),
    PHOTO(9, "PHOTO"),
    DOCUMENT(10, "DOCUMENT"),
    NOTE(11, "NOTE"),
    COMPUTED(12, "COMPUTED"),
    DATASET(13, "DATASET"),
    ADMINLEVEL(14, "ADMINLEVEL");

    private final int code;
    private final String label;

    AnswerType(int code, String label) {
        this.code = code;
        this.label = label;
    }

    public int getCode() { return code; }
    public String getLabel() {
        return label;
    }

    /** Look up by numeric code (1..13). */
    public static AnswerType fromCode(int code) {
        for (AnswerType t : values()) {
            if (t.code == code) return t;
        }
        throw new IllegalArgumentException("Unknown AnswerType code: " + code);
    }

    /** Flexible lookup by enum name or human label (case-insensitive, spaces/underscores ignored). */
    public static AnswerType fromString(String s) {
        if (s == null) throw new IllegalArgumentException("null");
        String key = s.trim().toUpperCase().replace(' ', '_');
        // try enum name first
        for (AnswerType t : values()) {
            if (t.name().equals(key)) return t;
        }
        // then try label normalized
        for (AnswerType t : values()) {
            if (t.label.toUpperCase().replace(' ', '_').equals(key)) return t;
        }
        throw new IllegalArgumentException("Unknown AnswerType: " + s);
    }

}
