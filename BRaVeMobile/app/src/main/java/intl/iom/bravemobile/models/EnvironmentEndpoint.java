package intl.iom.bravemobile.models;

public class EnvironmentEndpoint {
    public String environment;
    public String endpoint;

    public EnvironmentEndpoint(String dev, String url) {
        environment = dev;
        endpoint = url;
    }
}
