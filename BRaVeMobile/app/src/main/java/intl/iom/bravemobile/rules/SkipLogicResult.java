package intl.iom.bravemobile.rules;

import java.util.Collections;
import java.util.List;

public class SkipLogicResult {

    public boolean conditionTrue;
    public List<Integer> parentIds;

    /**
     * Result of evaluating skip logic: condition result and the list of parent question ids.
     */
    public SkipLogicResult(boolean conditionTrue, List<Integer> parentIds) {
        this.conditionTrue = conditionTrue;
        this.parentIds = (parentIds == null)
                ? Collections.emptyList()
                : parentIds;
    }
}
