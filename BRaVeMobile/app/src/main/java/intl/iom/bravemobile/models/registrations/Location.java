package intl.iom.bravemobile.models.registrations;

public class Location {
    public Location(int id, String name, int level, Integer parent, boolean isVisible, long lastUpdated) {
        this.id = id;
        this.name = name;
        this.level = level;
        this.parent = parent;
        this.isVisible = isVisible;
        this.lastUpdated = lastUpdated;
    }

    public Location(int id, String name, int level, Integer parent, boolean isVisible) {
        this.id = id;
        this.name = name;
        this.level = level;
        this.parent = parent;
        this.isVisible = isVisible;
    }

    public int id;
    public String name;
    public int level;

    public Integer parent;
    public boolean isVisible;
    public long lastUpdated;
}
