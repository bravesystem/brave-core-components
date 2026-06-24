package intl.iom.bravemobile.rules;

import androidx.annotation.Nullable;

import org.json.JSONArray;
import org.json.JSONObject;

import java.util.*;

import intl.iom.bravemobile.helpers.NumberUtils;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.statics.AnswerType;

import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

/**
 * Revised skip-logic engine that evaluates JSON conditions against answers,
 * with awareness of question types (AnswerType).
 *
 * Supported question types and operations:
 * - INT, NUMERIC, SELECT_ONE: EQ, NEQ, GT, GTE, LT, LTE (numeric comparison)
 * - TEXT, DATE, BOOLEAN: EQ, NEQ (string comparison)
 * - SELECT_MULTIPLE: IN (value in comma-separated integer list), NOT_IN (value not in list)
 * - Types 8-14 (GPS, PHOTO, etc.): ignored; rules referencing them evaluate to false.
 *
 * JSON shape:
 * - Group: { "type": "group", "op": "AND|OR|NOT", "expressions": [ ... ] }  (or "children")
 * - Rule:  { "type": "rule", "op": "...", "q": <questionId>, "value": "..." }
 *          For select_multiple list checks: "values": ["1","2"] is also supported for IN/NOT_IN.
 */
public final class RuleEngineRevised {

    private RuleEngineRevised() {}

    public static RestrictionResult validate(CollectionUnit dp, String answer)
    {
        if(StringUtils.isBlank(answer))
            return new RestrictionResult(true, null);

        if(dp.answerType == AnswerType.SELECT_MULTIPLE)
        {
            Number min = null, max = null;

            if(dp.minSelection.isPresent())
                min = dp.minSelection.get();

            if(dp.maxSelection.isPresent())
                max = dp.maxSelection.get();


            return checkRange(min, max, countNonEmpty(answer), "*");
        }

        // No restriction -> valid
        if (StringUtils.isBlank(dp.restriction)) {
            return new RestrictionResult(true, null);
        }

        // Deserialize config
        RestrictionConfig cfg = ObjectSerializer.deserialize(dp.restriction, RestrictionConfig.class);

        if (cfg == null) {
            // If config can't be read, don't block the user here
            return new RestrictionResult(true, null);
        }

        // If no value provided, treat as valid (required-ness should be handled elsewhere)
        if (StringUtils.isBlank(answer)) {
            return new RestrictionResult(true, null);
        }

        switch (dp.answerType)
        {
            case INT:
            case NUMERIC:

                final String trimmed = answer.trim();
                double value;
                try {
                    value = Double.parseDouble(trimmed);
                } catch (NumberFormatException nfe) {
                    return new RestrictionResult(false, "*");
                }

                // Min check (if provided)
                return checkRange(cfg.min, cfg.max, value, cfg.errorMessage);

            case TEXT:

                String pattern = cfg.pattern;

                if (StringUtils.isBlank(pattern)) {
                    return new RestrictionResult(true, null);
                }

                try {
                    boolean matches = Pattern.compile(pattern).matcher(answer).matches();
                    if (!matches) {
                        String msg = StringUtils.isBlank(cfg.errorMessage)
                                ? "Value does not match the required format"
                                : cfg.errorMessage;
                        return new RestrictionResult(false, msg);
                    }
                } catch (PatternSyntaxException pse) {
                    // Invalid regex in configuration: ignore the pattern (treat as valid)
                    return new RestrictionResult(true, null);
                }

                break;
        }

        return new RestrictionResult(true, null);

    }


    @Nullable
    private static RestrictionResult checkRange(Number min, Number max, Number value, String errorMessage) {
        double v = value.doubleValue();

        Double minD = (min != null) ? min.doubleValue() : null;
        Double maxD = (max != null) ? max.doubleValue() : null;

        boolean belowMin = (minD != null) && (v < minD);
        boolean aboveMax = (maxD != null) && (v > maxD);

        if (belowMin || aboveMax) {
            String msg;
            if (!StringUtils.isBlank(errorMessage)) {
                msg = errorMessage;
            } else if (minD != null && maxD != null) {
                msg = "Value should be between " + NumberUtils.formatNum(minD) + " and " + NumberUtils.formatNum(maxD) + ".";
            } else if (minD != null) {
                msg = "Value should be greater than or equal to " + NumberUtils.formatNum(minD) + ".";
            } else if (maxD != null) {
                msg = "Value should be less than or equal to " + NumberUtils.formatNum(maxD) + ".";
            } else {
                // No bounds supplied but flagged invalid -> generic fallback
                msg = "Value is out of the allowed range.";
            }
            return new RestrictionResult(false, msg);
        }

        return new RestrictionResult(true, null);
    }

    /**
     * Nicely format numbers (avoid scientific notation, trim trailing zeros).
     */
    /*private static String formatNum(double n) {
        // Use up to 15 significant digits and strip trailing zeros
        java.text.DecimalFormat df = new java.text.DecimalFormat("#.###############");
        df.setDecimalSeparatorAlwaysShown(false);
        return df.format(n);
    }*/

    /*@Nullable
    private static RestrictionResult checkRange(Number min, Number max, Number value, String errorMessage) {

        double v = value.doubleValue();

        boolean belowMin = (min != null) && (v < min.doubleValue());
        boolean aboveMax = (max != null) && (v > max.doubleValue());

        if (belowMin || aboveMax) {
            String msg = StringUtils.isBlank(errorMessage) ? "*" : errorMessage;
            return new RestrictionResult(false, msg);
        }
        return new RestrictionResult(true, null);
    }*/

    public static int countNonEmpty(String csv) {
        if (csv == null || csv.trim().isEmpty()) {
            return 0;
        }

        String[] parts = csv.split(",");
        int count = 0;

        for (String part : parts) {
            if (part != null && !part.trim().isEmpty()) {
                count++;
            }
        }

        return count;
    }


    /**
     * Evaluates skip logic JSON against the given question types and answers.
     *
     * @param dp             CollectionUnit (Question unit)
     * @param questionList  map of question id -> Full question context
     * @param answers        map of question id -> answer string (null or missing = no answer)
     * @return SkipLogicResult with condition result and list of parent question ids; never null
     */
    public static SkipLogicResult evaluate(CollectionUnit dp,
                                           Map<Integer, CollectionUnit> questionList,
                                           Map<Integer, String> answers) {
        String skipLogicJson = dp.skipLogic;

        if (skipLogicJson == null || skipLogicJson.trim().isEmpty()) {
            return new SkipLogicResult(false, Collections.emptyList());
        }
        Map<Integer, CollectionUnit> types = questionList != null ? questionList : new HashMap<>();
        Map<Integer, String> ans = answers != null ? answers : new HashMap<>();
        Set<Integer> parentIds = new HashSet<>();
        try {
            JSONObject node = new JSONObject(skipLogicJson.trim());
            JSONArray expressions = node.optJSONArray("expressions");
            if (expressions == null) expressions = node.optJSONArray("children");
            if (expressions == null || expressions.length() == 0) {
                dp.skipLogic = "";
                return new SkipLogicResult(false, Collections.emptyList());
            }

            boolean conditionTrue = evalNode(node, types, ans, 0, parentIds);
            return new SkipLogicResult(conditionTrue, new ArrayList<>(parentIds));
        } catch (Throwable t) {
            return new SkipLogicResult(false, new ArrayList<>(parentIds));
        }
    }

    private static final int MAX_DEPTH = 32;

    private static boolean evalNode(JSONObject node,
                                    Map<Integer, CollectionUnit> questionList,
                                    Map<Integer, String> answers,
                                    int depth, Set<Integer> parentIds) {
        if (node == null || depth > MAX_DEPTH) return false;

        String op = node.optString("op", "").trim();
        if (op.isEmpty()) return false;

        String opUpper = op.toUpperCase(Locale.ROOT);
        String type = node.optString("type", "").trim();

        // ----- Logical group (AND / OR / NOT) -----
        if ("AND".equals(opUpper) || "OR".equals(opUpper) || "NOT".equals(opUpper)) {
            JSONArray expressions = node.optJSONArray("expressions");
            if (expressions == null) expressions = node.optJSONArray("children");
            if (expressions == null) return false;

            if ("NOT".equals(opUpper)) {
                if (expressions.length() != 1) return false;
                JSONObject child = expressions.optJSONObject(0);
                return child != null && !evalNode(child, questionList, answers, depth + 1, parentIds);
            }

            if (expressions.length() == 0) return false;

            if ("AND".equals(opUpper)) {
                for (int i = 0; i < expressions.length(); i++) {
                    JSONObject c = expressions.optJSONObject(i);
                    if (c == null || !evalNode(c, questionList, answers, depth + 1, parentIds)) return false;
                }
                return true;
            } else {
                // OR
                for (int i = 0; i < expressions.length(); i++) {
                    JSONObject c = expressions.optJSONObject(i);
                    if (c != null && evalNode(c, questionList, answers, depth + 1, parentIds)) return true;
                }
                return false;
            }
        }

        // ----- Leaf rule -----
        Integer qId = getQuestionId(node);
        if (qId == null) return false;

        parentIds.add(qId);

        AnswerType answerType = questionList.get(qId).answerType;
        if (answerType == null) return false; // unknown question or type not provided

        int code = answerType.getCode();
        // Ignore types 8-14
        if (code >= 8 && code <= 14) return false;

        String answer = answers.get(qId);
        // Null or missing answer: condition not satisfied
        if (answer == null) answer = "";

        switch (answerType) {
            case INT:
            case NUMERIC:
            case SELECT_ONE:
                // SELECT_ONE options are represented as integer codes; treat like numeric
                return evalNumericRule(opUpper, node, answer);
            case TEXT:
            case DATE:
            case BOOLEAN:
                return evalTextRule(opUpper, node, answer);
            case SELECT_MULTIPLE:
                return evalSelectMultipleRule(opUpper, node, answer);
            default:
                return false;
        }
    }

    private static Integer getQuestionId(JSONObject node) {
        if (!node.has("q")) return null;
        try {
            return node.getInt("q");
        } catch (Exception ignored) {
            String s = node.optString("q", null);
            if (s == null) return null;
            try {
                return Integer.parseInt(s.trim());
            } catch (NumberFormatException e) {
                return null;
            }
        }
    }

    // ----- INT / NUMERIC: EQ, NEQ, GT, GTE, LT, LTE -----
    private static boolean evalNumericRule(String op, JSONObject node, String answer) {
        String valueStr = node.optString("value", null);
        Double answerNum = tryDouble(answer);
        Double valueNum = tryDouble(valueStr);
        if (answerNum == null || valueNum == null) {
            // Fallback: lexicographic or treat as not equal
            if ("EQ".equals(op)) return stringsEqual(answer, valueStr);
            if ("NEQ".equals(op)) return !stringsEqual(answer, valueStr);
            return false;
        }
        switch (op) {
            case "EQ":  return Double.compare(answerNum, valueNum) == 0;
            case "NEQ": return Double.compare(answerNum, valueNum) != 0;
            case "GT":  return answerNum > valueNum;
            case "GTE": return answerNum >= valueNum;
            case "LT":  return answerNum < valueNum;
            case "LTE": return answerNum <= valueNum;
            default:    return false;
        }
    }

    // ----- TEXT, DATE, BOOLEAN, SELECT_ONE: EQ, NEQ -----
    private static boolean evalTextRule(String op, JSONObject node, String answer) {
        String value = node.optString("value", null);
        if (value == null) value = "";
        boolean eq = stringsEqual(answer, value);
        switch (op) {
            case "EQ":  return eq;
            case "NEQ": return !eq;
            default:    return false;
        }
    }

    // ----- SELECT_MULTIPLE: IN (value in list), NOT_IN (value not in list). Answer is comma-separated integers. -----
    private static boolean evalSelectMultipleRule(String op, JSONObject node, String answer) {
        Set<String> answerSet = splitByComma(answer);
        // Single value in "value", or multiple in "values"
        Set<String> toCheck = new HashSet<>();
        if (node.has("values")) {
            JSONArray arr = node.optJSONArray("values");
            if (arr != null) {
                for (int i = 0; i < arr.length(); i++) {
                    String v = optString(arr, i);
                    if (v != null && !v.isEmpty()) toCheck.add(v.trim());
                }
            }
        }
        String single = node.optString("value", null);
        if (single != null && !single.trim().isEmpty()) toCheck.add(single.trim());
        if (toCheck.isEmpty()) return false;

        switch (op) {
            case "IN":
            case "CONTAINS":
                // At least one of toCheck is in the answer list
                for (String v : toCheck) if (answerSet.contains(v)) return true;
                return false;
            case "NOT_IN":
                // None of toCheck may be in the answer list
                for (String v : toCheck) if (answerSet.contains(v)) return false;
                return true;
            default:
                return false;
        }
    }

    private static Set<String> splitByComma(String raw) {
        Set<String> set = new HashSet<>();
        if (raw == null || raw.isEmpty()) return set;
        for (String part : raw.split(",")) {
            String t = part.trim();
            if (!t.isEmpty()) set.add(t);
        }
        return set;
    }

    private static String optString(JSONArray arr, int index) {
        try {
            Object o = arr.get(index);
            return o == null || o == JSONObject.NULL ? null : String.valueOf(o).trim();
        } catch (Exception e) {
            return null;
        }
    }

    private static boolean stringsEqual(String a, String b) {
        String sa = a == null ? "" : a.trim();
        String sb = b == null ? "" : (b instanceof String ? ((String) b).trim() : String.valueOf(b).trim());
        return sa.equals(sb);
    }

    private static Double tryDouble(Object o) {
        if (o == null) return null;
        if (o instanceof Number) return ((Number) o).doubleValue();
        try {
            return Double.parseDouble(String.valueOf(o).trim());
        } catch (Exception e) {
            return null;
        }
    }
}
