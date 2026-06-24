package intl.iom.bravemobile.interfaces;

import java.util.List;

import intl.iom.bravemobile.helpers.SelectItem;

public interface OptionsProvider {
    /** Return the list of option labels for SELECT_ONE / SELECT_MULTIPLE. */
    List<SelectItem> getOptionsFor(int lookupId);

}