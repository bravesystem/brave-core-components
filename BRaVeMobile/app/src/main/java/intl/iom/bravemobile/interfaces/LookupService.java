package intl.iom.bravemobile.interfaces;

import android.os.Build;

import java.util.List;
import java.util.Optional;

import intl.iom.bravemobile.exceptions.LookupItemNotFound;
import intl.iom.bravemobile.helpers.SelectItem;

public interface LookupService {

    void loadAll();
    Optional<List<SelectItem>> getLookupItemList(int lookupId);
    Optional<List<SelectItem>> getLookupItemList(String lookupName);
    default String getLookupItemLabel(int itemId, String lookupName) throws LookupItemNotFound {

        Optional<List<SelectItem>> lookups = getLookupItemList(lookupName);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            if(lookups.isPresent())
            {
                for(SelectItem item: lookups.get())
                {
                    if(item.getValue()==itemId)
                        return item.getLabel();
                }
            }
        }

        throw  new LookupItemNotFound("The requested item (ID %d) could not be found in %s.");
    }
    default String getLookupItemLabel(int itemId, int lookupId) throws LookupItemNotFound {

        Optional<List<SelectItem>> lookups = getLookupItemList(lookupId);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            if(lookups.isPresent())
            {
                for(SelectItem item: lookups.get())
                {
                    if(item.getValue()==itemId)
                        return item.getLabel();
                }
            }
        }

        throw  new LookupItemNotFound("The requested item (ID %d) could not be found in %s.");
    }

}
