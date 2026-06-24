package intl.iom.bravemobile.models.jwt;

import java.util.UUID;

public class IntegrityTokenRequestDto {
    private UUID id;
    private String token;

    public IntegrityTokenRequestDto(UUID id,String token) {
        this.id = id;
        this.token = token;
    }

    public String getToken() {
        return token;
    }
    public UUID getNonceId() {
        return id;
    }
}