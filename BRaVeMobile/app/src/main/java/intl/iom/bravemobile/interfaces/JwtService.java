package intl.iom.bravemobile.interfaces;

import intl.iom.bravemobile.models.jwt.JwtRefreshPair;

public interface JwtService {
    JwtRefreshPair getJwt(String environment);
    void saveJwt(String environment, String jwt, String refresh);
}



