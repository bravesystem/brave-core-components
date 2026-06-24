package intl.iom.bravemobile.rules;

import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

/**
 * First-draft algorithm for applying skip-logic visibility to a list of ViewHolders.
 *
 * Assumptions:
 * - ViewHolder is a basic Java class with at least:
 *      int getId();
 *      String getSkipLogic();
 * - The input list is already sorted by id in ascending order.
 * - A separate function can evaluate skipLogic, returning:
 *      - whether the parents' conditions are satisfied
 *      - the list of parent question ids referenced in that skipLogic
 *
 * This class only contains the control-flow algorithm.
 * The concrete implementations of show/hide/evaluate are left abstract.
 */
public interface SkipLogicVisibility
{
    public void apply();
}

