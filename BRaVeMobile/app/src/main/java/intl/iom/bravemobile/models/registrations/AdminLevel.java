package intl.iom.bravemobile.models.registrations;

public class AdminLevel {
    public AdminLevel(int id, String name, boolean isVisible, boolean isRequired, long lastUpdated) {
        this.id = id;
        this.name = name;
        this.isVisible = isVisible;
        this.isRequired = isRequired;
        this.lastUpdated = lastUpdated;
    }

    public AdminLevel(int id, String name, boolean isVisible, boolean isRequired) {
        this.id = id;
        this.name = name;
        this.isVisible = isVisible;
        this.isRequired = isRequired;
    }

    public int id;
    public String name;
    public boolean isVisible;
    public boolean isRequired;
    public long lastUpdated;
}
