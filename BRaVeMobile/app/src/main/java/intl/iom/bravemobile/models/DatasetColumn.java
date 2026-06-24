package intl.iom.bravemobile.models;

public class DatasetColumn {
    public int id ;
    public String title;
    public int type;
    public boolean required ;
    public Integer lookupId;

    public DatasetColumn(int id, String title, int type, boolean required) {
        this.id = id;
        this.title = title;
        this.type = type;
        this.required = required;
    }

    public DatasetColumn(int id, String title, int type, boolean required, Integer lookupId) {
        this(id, title, type, required);
        this.lookupId = lookupId;
    }
}
