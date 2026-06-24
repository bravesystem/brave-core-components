package intl.iom.bravemobile.models.jwt;

import com.google.gson.annotations.SerializedName;

import java.util.UUID;

public class NonceResponse
{
    @SerializedName("nonceId")
    public UUID id;
    public String nonce;
}
