package intl.iom.bravemobile.rules;

import android.os.Build;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.*;
import java.util.function.BiFunction;

/**
 * SkipLogicEngine
 * ----------------
 * - Evaluates JSON conditions against Map<Integer,String> answers.
 * - JSON shape:
 *     Logical: { "op": "AND|OR|NOT", "children": [ <node>, ... ] }
 *     Leaf   : { "op": "EQ|NEQ|GT|GTE|LT|LTE|IN|COUNT_GTE|EXISTS|CONTAINS", "q": 5, ... }
 * - Select-many answers are comma-separated strings (e.g., "2,5,7").
 * - Fail-safe: never throws; errors reported via ErrorReporter and result defaults to false.
 * - Extensible: register new operators via register("OP", handler).
 */
public final class RuleEngine {

    private static String TAG = RuleEngine.class.getSimpleName();

    private RuleEngine() {}

    // ---------------- Configuration ----------------
    private static final int MAX_DEPTH = 32;

    /** Centralized error reporter; swap with Crashlytics/etc. */
    public interface ErrorReporter {
        void report(String code, String detail, String jsonSnippet, Throwable t);
    }

    private static volatile ErrorReporter reporter = (code, detail, js, t) ->
            android.util.Log.e(TAG, code + " • " + detail + " • " + js, t);

    public static void setErrorReporter(ErrorReporter r) {
        reporter = (r != null ? r : reporter);
    }

    // ---------------- Public APIs ----------------

    /** Boolean API. On any parse/eval error, returns false (safe default). */
    public static boolean evaluate(String skipLogicJson, Map<Integer, String> answers) {
        if (skipLogicJson == null || skipLogicJson.trim().isEmpty()) return false; // no logic => not required
        try {
            JSONObject node = new JSONObject(skipLogicJson);
            return evalNode(node, answers, 0);
        } catch (Throwable ex) {
            reporter.report("E_PARSE", ex.getMessage(), truncate(skipLogicJson), ex);
            return false;
        }
    }

    /** Detailed API with error info. */
    public static EvalResult evaluateSafe(String skipLogicJson, Map<Integer,String> answers) {
        if (skipLogicJson == null || skipLogicJson.trim().isEmpty()) return EvalResult.ok(false);
        try {
            JSONObject node = new JSONObject(skipLogicJson);
            boolean ok = evalNode(node, answers, 0);
            return ok ? EvalResult.ok(true) : new EvalResult(false, "E_FALSE", "Condition evaluated to false");
        } catch (org.json.JSONException ex) {
            reporter.report("JSON_SYNTAX", ex.getMessage(), truncate(skipLogicJson), ex);
            return new EvalResult(false, "JSON_SYNTAX", ex.getMessage());
        } catch (IllegalArgumentException ex) {
            reporter.report("BAD_NODE", ex.getMessage(), truncate(skipLogicJson), ex);
            return new EvalResult(false, "BAD_NODE", ex.getMessage());
        } catch (UnsupportedOperationException ex) {
            reporter.report("UNSUPPORTED_OP", ex.getMessage(), truncate(skipLogicJson), ex);
            return new EvalResult(false, "UNSUPPORTED_OP", ex.getMessage());
        } catch (StackOverflowError err) {
            reporter.report("STACK_OVERFLOW", "Likely excessive depth/recursion", "", err);
            return new EvalResult(false, "STACK_OVERFLOW", "Depth exceeded");
        } catch (Throwable t) {
            reporter.report("UNKNOWN", t.getMessage(), "", t);
            return new EvalResult(false, "UNKNOWN", t.getMessage());
        }
    }

    /** Light shape sanity check (use when saving rules). */
    public static boolean validateShape(String json) {
        try {
            JSONObject n = new JSONObject(json);
            String op = n.optString("op", "");
            if (op.isEmpty()) return false;

            String OP = op.toUpperCase(Locale.ROOT);
            if (OP.equals("AND") || OP.equals("OR") || OP.equals("NOT")) {
                JSONArray arr = n.optJSONArray("children");
                if (arr == null) return false;
                if (OP.equals("NOT")) return arr.length() == 1 && arr.optJSONObject(0) != null;
                return arr.length() >= 1;
            } else {
                if (!n.has("q")) return false;
                // leaf must have one of these depending on op
                return n.has("value") || n.has("values") || n.has("range");
            }
        } catch (Exception e) {
            return false;
        }
    }

    /** Register (or override) an operator. Always wrapped so it cannot throw. */
    public static void register(String opName, BiFunction<JSONObject, Map<Integer,String>, Boolean> op) {
        OPS.put(opName.toUpperCase(Locale.ROOT), wrapNoThrow(opName, op));
    }

    // ---------------- Result type ----------------
    public static final class EvalResult {
        public final boolean ok;
        public final String errorCode;  // null if ok
        public final String message;    // null if ok
        private EvalResult(boolean ok, String code, String msg) { this.ok = ok; this.errorCode = code; this.message = msg; }
        public static EvalResult ok(boolean val) { return new EvalResult(val, null, null); }
    }

    // ---------------- Core evaluator (NEVER throws) ----------------
    private static boolean evalNode(JSONObject node, Map<Integer, String> answers, int depth) {
        if (node == null) { reporter.report("E_NULL_NODE", "node is null", "", null); return false; }
        if (depth > MAX_DEPTH) { reporter.report("E_DEPTH", "Max depth exceeded", node.toString(), null); return false; }

        try {
            final String op = node.optString("op", null);
            final String type = node.optString("type", null);
            if (op == null || op.isEmpty()) {
                reporter.report("E_MISSING_OP", "Node missing 'op'", node.toString(), null);
                return false;
            }
            final String OP = op.toUpperCase(Locale.ROOT);

            // Composition
            if (OP.equals("AND") || OP.equals("OR") || OP.equals("NOT")) {

                JSONArray children = node.optJSONArray("children");

                if (children == null) {
                    children = node.optJSONArray("expressions");
                }

                if ( !type.equalsIgnoreCase("group") && children == null) {
                    reporter.report("E_CHILDREN", OP + " missing 'children'", node.toString(), null);
                    return false;
                }
                if (OP.equals("NOT")) {
                    if (children.length() != 1) {
                        reporter.report("E_CHILDREN_COUNT", "NOT requires exactly one child", node.toString(), null);
                        return false;
                    }
                    JSONObject only = children.optJSONObject(0);
                    if (only == null) {
                        reporter.report("E_CHILD_NULL", "NOT child not an object", node.toString(), null);
                        return false;
                    }
                    return !evalNode(only, answers, depth + 1);
                }
                if (children!=null && children.length() == 0) {
                    reporter.report("E_CHILDREN_COUNT", OP + " requires >= 1 child", node.toString(), null);
                    return false;
                }
                if (OP.equals("AND")) {
                    for (int i = 0; children!=null && i < children.length(); i++) {
                        JSONObject c = children.optJSONObject(i);
                        if (c == null) {
                            reporter.report("E_CHILD_NULL", "AND child not an object at " + i, node.toString(), null);
                            return false;
                        }
                        if (!evalNode(c, answers, depth + 1)) return false; // short-circuit
                    }
                    return true;
                } else { // OR
                    for (int i = 0; children!=null && i < children.length(); i++) {
                        JSONObject c = children.optJSONObject(i);
                        if (c == null) {
                            reporter.report("E_CHILD_NULL", "OR child not an object at " + i, node.toString(), null);
                            continue; // treat as false
                        }
                        if (evalNode(c, answers, depth + 1)) return true; // short-circuit
                    }
                    return false;
                }
            }

            // Leaf
            BiFunction<JSONObject, Map<Integer,String>, Boolean> fn = OPS.get(OP);
            if (fn == null) {
                reporter.report("E_UNSUPPORTED_OP", OP, node.toString(), null);
                return false;
            }
            if (!node.has("q")) {
                reporter.report("E_MISSING_Q", "Leaf missing 'q' (question id)", node.toString(), null);
                return false;
            }
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                return fn.apply(node, answers); // wrapped -> never throws
            }

        } catch (Throwable ex) {
            reporter.report("E_EVAL", "Unexpected exception in evalNode", node.toString(), ex);

        }

        return false;
    }

    // ---------------- Operator registry (wrapped, no-throw) ----------------
    private static final Map<String, BiFunction<JSONObject, Map<Integer,String>, Boolean>> OPS = new HashMap<>();
    static {
        // Equality / inequality
        register("EQ",  (n,ctx) -> {

            Integer q = getQ(n);
            if (q == null) return false;  // already reported

            return eq(getAns(ctx, q), n.optString("value", null));

        });
        register("NEQ", (n,ctx) -> {

            Integer q = getQ(n);
            if (q == null) return false;  // already reported

            return !eq(getAns(ctx, q), n.optString("value", null));

        });

        // Numeric/lexicographic compares
        register("GT",  (n,ctx) -> {

            Integer q = getQ(n);
            if (q == null) return false;  // already reported

            return cmp(getAns(ctx, q), n.optString("value", null), (a,b)-> a >  b);

        });
        register("GTE", (n,ctx) -> {

            Integer q = getQ(n);
            if (q == null) return false;  // already reported

            return cmp(getAns(ctx, getQ(n)), n.optString("value", null), (a,b)-> a >= b);
        });
        register("LT",  (n,ctx) -> {

            Integer q = getQ(n);
            if (q == null) return false;  // already reported

            return cmp(getAns(ctx, q), n.optString("value", null), (a,b)-> a <  b);
        });
        register("LTE", (n,ctx) -> {

            Integer q = getQ(n);

            if (q == null) return false;  // already reported

            return cmp(getAns(ctx, q), n.optString("value", null), (a,b)-> a <= b);});

        // Exists / contains
        register("EXISTS",   (n,ctx) -> {

            Integer q = getQ(n);

            if (q == null) return false;  // already reported

            return notBlank(getAns(ctx, q));
        });

        register("CONTAINS", (n,ctx) -> {

            Integer q = getQ(n);

            if (q == null) return false;  // already reported

            if (android.os.Build.VERSION.SDK_INT >= android.os.Build.VERSION_CODES.N) {
                String hay = Optional.ofNullable(getAns(ctx, q)).orElse("");
                String needle = Optional.ofNullable(n.optString("value", "")).orElse("");
                return hay.toLowerCase(Locale.ROOT).contains(needle.toLowerCase(Locale.ROOT));
            }

            return false;
        });

        // Select-many helpers (answers like "2,5,7")
        register("IN", (n,ctx) -> {

            Integer q = getQ(n);

            if (q == null) return false;  // already reported

            Set<String> actual = splitMany(getAns(ctx, q));
            Set<String> wanted = jsonArrayToSet(n.optJSONArray("values"));
            for (String w : wanted) if (actual.contains(w)) return true;
            return false;
        });
        register("COUNT_GTE", (n,ctx) -> {

            Integer q = getQ(n);

            if (q == null) return false;  // already reported

            int want = safeInt(n.opt("value"), 0);
            int count = splitMany(getAns(ctx, q)).size();
            return count >= want;
        });
    }

    /** Wrap an operator so it cannot throw */
    private static BiFunction<JSONObject, Map<Integer,String>, Boolean> wrapNoThrow(
            String name, BiFunction<JSONObject, Map<Integer,String>, Boolean> fn) {
        return (node, ctx) -> {
            try {
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                    return fn.apply(node, ctx);
                }
            } catch (Throwable t) {
                reporter.report("E_OP_RUNTIME", "Operator failed: " + name, node.toString(), t);

            }

            return false;
        };
    }

    // ---------------- Helpers ----------------
    private static Integer getQ(JSONObject node) {
        // 1) Present?
        if (!node.has("q")) {
            reporter.report("E_MISSING_Q", "Leaf missing 'q' (question id)", node.toString(), null);
            return null;
        }

        // 2) Fast path: real int
        try {
            return node.getInt("q");
        } catch (Exception ignored) {
            // fall through to tolerant parsing
        }

        // 3) Tolerate string "5"
        String qStr = node.optString("q", null);
        if (qStr != null) {
            try {
                return Integer.parseInt(qStr.trim());
            } catch (NumberFormatException nfe) {
                reporter.report("E_Q_TYPE", "Field 'q' is not an int: " + qStr, node.toString(), nfe);
                return null;
            }
        }

        // 4) Any other type
        reporter.report("E_Q_TYPE", "Field 'q' must be int or numeric string", node.toString(), null);
        return null;

    }
    private static String getAns(Map<Integer, String> ctx, int q) { return ctx.get(q); }

    private static boolean notBlank(String s) { return s != null && s.trim().length() > 0; }

    private static boolean eq(String a, String b) {
        Double da = tryDouble(a), db = tryDouble(b);
        if (da != null && db != null) return Double.compare(da, db) == 0;
        String sa = (a == null ? "" : a), sb = (b == null ? "" : b);
        return sa.equals(sb);
    }

    private interface DCmp { boolean go(double a, double b); }
    private static boolean cmp(String a, String b, DCmp cmp) {
        Double da = tryDouble(a), db = tryDouble(b);
        if (da != null && db != null) return cmp.go(da, db);
        String sa = (a == null ? "" : a), sb = (b == null ? "" : b);
        int rel = sa.compareTo(sb); // lexicographic fallback
        return cmp.go(rel, 0);
    }

    private static Double tryDouble(Object o) {
        if (o == null) return null;
        if (o instanceof Number) return ((Number)o).doubleValue();
        try { return Double.parseDouble(String.valueOf(o)); }
        catch (Exception e) { return null; }
    }

    private static int safeInt(Object o, int def) {
        if (o == null) return def;
        try { return Integer.parseInt(String.valueOf(o)); }
        catch (Exception e) { return def; }
    }

    private static Set<String> splitMany(String raw) {
        if (raw == null || raw.trim().isEmpty()) return new LinkedHashSet<>();
        String[] parts = raw.split(",");
        LinkedHashSet<String> set = new LinkedHashSet<>();
        for (String p : parts) {
            String t = p.trim();
            if (!t.isEmpty()) set.add(t);
        }
        return set;
    }

    private static Set<String> jsonArrayToSet(JSONArray arr) {
        LinkedHashSet<String> set = new LinkedHashSet<>();
        if (arr == null) return set;

        for (int i = 0, n = arr.length(); i < n; i++) {
            Object v = arr.opt(i);                   // safe (no JSONException)
            if (v == null || v == JSONObject.NULL) continue;

            String s = String.valueOf(v).trim();    // handles numbers/booleans/strings
            if (!s.isEmpty()) set.add(s);           // ignore blanks
        }
        return set;
    }


    private static String truncate(String s) {
        if (s == null) return "";
        return (s.length() > 400) ? s.substring(0, 400) + "…" : s;
    }
}
