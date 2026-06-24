package intl.iom.bravemobile.interfaces;

public interface AppConfigProvider {

    String getString(String key, String defaultValue);
    int getInt(String key, int defaultValue);
    boolean getBoolean(String key, boolean defaultValue);

}
