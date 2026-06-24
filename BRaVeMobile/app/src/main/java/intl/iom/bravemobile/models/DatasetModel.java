package intl.iom.bravemobile.models;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public class DatasetModel
{
    public String title;
    public String description;
    public List<DatasetColumnModel> columns;

    public class DatasetColumnModel {
        public int id ;
        public String title;
        public String type;
        public boolean required ;
        public Integer lookupId;
    }

    public List<DatasetColumn> getDatasetColumns()
    {
        List<DatasetColumn> ds = new ArrayList<>();
        for(DatasetColumnModel c : columns)
        {
            ds.add(new DatasetColumn(c.id, c.title,mapType(c.type),c.required,c.lookupId));
        }

        return ds;
    }

    private static int mapType(String t) {
        if (t == null) return 3; // default to text
        switch (t.toLowerCase(Locale.ROOT)) {
            case "int":
            case "integer": return 1;
            case "numeric": return 2;
            case "text": return 3;
            case "date": return 4;
            case "boolean": return 5;
            case "select": return 6;
            default: return 3; // fallback to text
        }
    }

}


