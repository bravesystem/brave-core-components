package intl.iom.bravemobile.api;

import java.util.List;

import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.models.ExtendExpiryRequest;
import intl.iom.bravemobile.models.PublicKeyResponse;
import intl.iom.bravemobile.models.SetPinRequest;
import intl.iom.bravemobile.models.activities.RegistrationActivityModel;
import intl.iom.bravemobile.models.distributions.DataPacket;
import intl.iom.bravemobile.models.distributions.EnrollmentRequest;
import intl.iom.bravemobile.models.distributions.EnrollmentResponse;
import intl.iom.bravemobile.models.jwt.ClaimRequest;
import intl.iom.bravemobile.models.jwt.ClaimResponse;
import intl.iom.bravemobile.models.jwt.IntegrityTokenRequestDto;
import intl.iom.bravemobile.models.jwt.NonceResponse;
import intl.iom.bravemobile.models.jwt.VerifyResponse;
import intl.iom.bravemobile.models.registrations.FlaggedData;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.VerifyTemplatesRequest;
import intl.iom.bravemobile.models.registrations.VerificationResponse;
import okhttp3.RequestBody;
import okhttp3.ResponseBody;
import retrofit2.Call;
import retrofit2.http.Body;
import retrofit2.http.GET;
import retrofit2.http.Headers;
import retrofit2.http.POST;
import retrofit2.http.Path;
import retrofit2.http.Query;

public interface EnrolledApis {

    @GET("api/v1/nonce/generate/{deviceId}")
    Call<NonceResponse> getNonce(@Path("deviceId") String deviceId);

    @POST("api/v1/integrity/verify") // Adjust path if needed
    Call<VerifyResponse> verifyToken(@Body IntegrityTokenRequestDto request);

    @POST("api/v1/enrollclaim") // Adjust path if needed
    Call<ClaimResponse> enroll(@Body ClaimRequest request);


    @POST("api/v1/enumerators/setpin") // Adjust path if needed
    Call<List<Enumerator>> setPin(@Body SetPinRequest request);

    @POST("api/v1/enumerators/extendexpiry") // Adjust path if needed
    Call<List<Enumerator>> extendExpiry(@Body ExtendExpiryRequest request);

    @GET("api/v1/pubkey") // Adjust path if needed
    Call<PublicKeyResponse> getPubKey();

    @GET("api/v1/enumerators/refresh/{Flag}") // Adjust path if needed
    Call<List<Enumerator>> getEnumeratorList(@Path("Flag") int Flag);


    //@GET("api/v1/registrationactivities/download/{tenantId}/{activityId}/") // Adjust path if needed
    //Call<RegistrationActivityModel> getActivity(@Path("tenantId") int tenantId, @Path("activityId") String activityId);

    @GET("api/v1/registrationactivities/download/{tenantId}/{activityId}/")
    Call<RegistrationActivityModel> getActivity(
            @Path("tenantId") int tenantId,
            @Path("activityId") String activityId,
            @Query("lang") String lang
    );


    @Headers("Content-Type: application/x-ndjson")
    @POST("api/v1/registrationactivities/push")
    Call<ResponseBody> postRegistration(@Body RequestBody ndjsonBody);

    @POST("api/v1/registrationactivities/verify/templates")
    Call<List<VerificationResponse>> verifyTemplates(@Body VerifyTemplatesRequest request);

    @POST("api/v1/registrationactivities/enrollments/fetch")
    Call<EnrollmentResponse> fetchEnrollmentData(@Body DataPacket request);

    @GET("api/pubkey")
    Call<PublicKeyResponse> getPubKeyB64();

    @GET("api/ping")
    Call<Boolean> basicPing();

    @GET("api/ping/secure")
    Call<Boolean> securePing();

    @POST("api/v1/registrationactivities/download/flaggeddata/{tenantId}/{partitionCode}")
    Call<FlaggedData> downloadFlaggedData(
            @Path("tenantId") int tenantId,
            @Path("partitionCode") String partitionCode
    );

}
