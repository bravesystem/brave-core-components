package intl.iom.bravemobile.models;

public class CustomKeyPairGroup {
    public int id;
    public int group_id;
    public String name;
    public boolean isActive;

    public CustomKeyPairGroup(int id, int group_id, String name, boolean isActive) {
        this.id = id;
        this.group_id = group_id;
        this.name = name;
        this.isActive = isActive;
    }
}
