package intl.iom.bravemobile.helpers;

import intl.iom.bravemobile.statics.Env;

public final class EnvParsers {
    private EnvParsers() {}

    private static final java.util.Map<String, String> ALIASES = new java.util.HashMap<>();
    static {
        // PROD
        ALIASES.put("PROD", Env.PROD);
        ALIASES.put("PRODUCTION", Env.PROD);
        ALIASES.put("LIVE", Env.PROD);

        // UAT
        ALIASES.put("UAT", Env.UAT);
        ALIASES.put("STAGING", Env.UAT);
        ALIASES.put("PREPROD", Env.UAT);

        // DEV
        ALIASES.put("DEV", Env.DEV);
        ALIASES.put("DEVELOPMENT", Env.DEV);
        ALIASES.put("TEST", Env.DEV);
    }

    /** Parses with aliases; falls back to defaultEnv if unknown. */
    public static String parse(String text, String defaultEnv) {
        if (text == null) return defaultEnv;
        String key = normalize(text);
        String env = ALIASES.get(key);
        return env != null ? env : defaultEnv;
    }

    private static String normalize(String s) {
        // Trim, uppercase, collapse separators
        String k = s.trim().toUpperCase(java.util.Locale.ROOT);
        k = k.replace('-', ' ').replace('_', ' ');
        k = k.replaceAll("\\s+", "");
        return k; // e.g., "pre-prod" -> "PREPROD"
    }
}
