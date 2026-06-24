package intl.iom.bravemobile.helpers;

import java.util.HashSet;
import java.util.Map;
import java.util.Set;
import java.util.regex.Pattern;
import java.util.regex.PatternSyntaxException;

import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.rules.RestrictionConfig;
import intl.iom.bravemobile.rules.RuleEngineRevised;
import intl.iom.bravemobile.rules.SkipLogicResult;
import intl.iom.bravemobile.statics.AnswerType;

public final class DataCollectionUnitValidator {

    private static String TAG = DataCollectionUnitValidator.class.getSimpleName();

    public static boolean validate(CollectionUnit dp, Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, CollectionUnit> questionTypes , Map<Integer, String> answers, boolean isRequired)
    {

        RestrictionConfig cfg = null;

        //answer to question id = dp.id
        String answer = answers.get(dp.id);

        //get question dependencies
        //RuleEngineRevised.QuestionDependencies parents = new RuleEngineRevised.QuestionDependencies();

        //boolean result = RuleEngine.evaluate(dp.skipLogic, answers);
        //boolean result = RuleEngineRevised.evaluate(dp, questionTypes,answers, parents);

        SkipLogicResult skipLogicResult = RuleEngineRevised.evaluate(dp, questionTypes,answers);

        boolean result = skipLogicResult.conditionTrue;

        /*CollectionUnitAdapter.ConstraintValidator holder = viewHolders.get(dp.id);

        if (holder != null) {
            // If rule evaluates to false AND this question depends on at least one parent,
            // hide the view; otherwise show it.
            if (!result && !parents.getIds().isEmpty()) {
                holder.hide();
            } else {
                holder.show();
            }
        }*/

        if (!result && !skipLogicResult.parentIds.isEmpty())
           answers.put(dp.id, null);

        if(StringUtils.isBlank( answer ) && isRequired)
        {
            if( result )
                return false;

            if( StringUtils.isBlank(dp.skipLogic) )
                return false;

        }

        if( !StringUtils.isBlank(dp.restriction))
        {
            // Deserialize config
            cfg = ObjectSerializer.deserialize(dp.restriction, RestrictionConfig.class);
        }

        if( cfg!=null)
        {
            switch (dp.answerType)
            {
                case INT:
                case NUMERIC:

                    final String trimmed = answer.trim();
                    double value;
                    try {
                        value = Double.parseDouble(trimmed);
                    } catch (NumberFormatException nfe) {
                        return false;//new RestrictionResult(false, "*");
                    }

                    // Min check (if provided)
                    return checkRange(viewHolders.get(dp.id),cfg.min, cfg.max, value, cfg.errorMessage);

                case TEXT:

                    String pattern = cfg.pattern;

                    if (StringUtils.isBlank(pattern)) {
                        return true;//new RestrictionResult(true, null);
                    }

                    try {
                        boolean matches = Pattern.compile(pattern).matcher(answer).matches();
                        if (!matches) {
                            String msg = StringUtils.isBlank(cfg.errorMessage)
                                    ? "Value does not match the required format"
                                    : cfg.errorMessage;

                            viewHolders.get(dp.id).setValidation(msg, false);
                            return  false;//new RestrictionResult(false, msg);
                        }
                    } catch (PatternSyntaxException pse) {
                        // Invalid regex in configuration: ignore the pattern (treat as valid)
                        return true;//new RestrictionResult(true, null);
                    }

                    break;
            }
        }


        if(StringUtils.isBlank( answer ) && !isRequired)
            return true;


        if(result && dp.answerType== AnswerType.SELECT_MULTIPLE && !selectMultipleValidate(dp, answer))
            return false;


        return true;
    }

    private static boolean checkRange(CollectionUnitAdapter.ConstraintValidator constraintValidator, Number min, Number max, Number value, String errorMessage) {
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
            constraintValidator.setValidation(msg, false);
            return false;
        }

        return true;
    }

    private static boolean selectMultipleValidate(CollectionUnit dp, String answer) {

        // restore existing selections
        Set<Integer> selected = new HashSet<>();

        for (String o : getOptions(answer)) {
            if (o != null) selected.add(Integer.parseInt(o));
        }

        int size = selected.size();

        // If neither min nor max is set, require at least one selection
        if (!dp.minSelection.isPresent() && !dp.maxSelection.isPresent()) {
            return size > 0;
        }

        // Boundaries with sensible defaults
        int min = dp.minSelection.orElse(0);
        int max = dp.maxSelection.orElse(Integer.MAX_VALUE);
        if (max < min) max = min; // guard against bad config

        if(size < min || size > max)
            return false;


        return true;
    }
    private static String[] getOptions(String existing) {
        if(existing==null || existing.trim().isEmpty())
            return new String[0];

        return existing.trim().split(",");
    }


}
