package intl.iom.bravemobile.models;

import android.content.ContentValues;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.helpers.SelectItem;

public class CustomLookup
{
    public int id;
    public String name;
    public boolean isActive;
    public List<CustomKeyPair> values;

    public CustomLookup(int id, String name, List<CustomKeyPair> values)
    {
        this.id = id;
        this.name = name;
        this.values = values;
    }

    public List<CustomKeyPairGroup> getLkpValues()
    {
        List<CustomKeyPairGroup> lkps = new ArrayList<>();

        for(CustomKeyPair g : values)
        {
            lkps.add(new CustomKeyPairGroup(g.id, id,g.name, g.isActive));
        }

        return lkps;
    }
}
