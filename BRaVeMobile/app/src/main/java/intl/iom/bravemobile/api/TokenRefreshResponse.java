package intl.iom.bravemobile.api;

public class TokenRefreshResponse {
    private String jwt = "";
    private String refresh = "";

    // Getters and Setters
    public String getJwt() {
        return jwt;
    }

    public void setJwt(String jwt) {
        this.jwt = jwt;
    }

    public String getRefresh() {
        return refresh;
    }

    public void setRefresh(String refresh) {
        this.refresh = refresh;
    }
}