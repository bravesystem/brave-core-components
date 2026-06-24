package intl.iom.bravemobile.ui.activities;

import androidx.appcompat.app.AppCompatActivity;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.ListView;
import android.widget.TextView;
import android.widget.Toast;

import com.google.android.material.floatingactionbutton.FloatingActionButton;
import com.google.gson.Gson;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

import java.io.IOException;
import java.security.GeneralSecurityException;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.RegistrationActivityAdapter;
import intl.iom.bravemobile.api.EnrolledApis;
import intl.iom.bravemobile.exceptions.AuthException;
import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.helpers.AESHelper;
import intl.iom.bravemobile.helpers.AESPacket;
import intl.iom.bravemobile.helpers.AlertDialogUtils;
import intl.iom.bravemobile.helpers.DataCollectionUtils;
import intl.iom.bravemobile.helpers.DialogLoadingHost;
import intl.iom.bravemobile.helpers.KeyStoreHelper;
import intl.iom.bravemobile.helpers.LanguageHelper;
import intl.iom.bravemobile.helpers.NonceUtil;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.RegistrationActivityValidator;
import intl.iom.bravemobile.helpers.WithLoading;
import intl.iom.bravemobile.interfaces.BasicCallback;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.interfaces.RegistrationActivityService;
import intl.iom.bravemobile.models.Enumerator;
import intl.iom.bravemobile.models.PinVerificationResult;
import intl.iom.bravemobile.models.PublicKeyResponse;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.activities.ConsentsFeedback;
import intl.iom.bravemobile.models.activities.ConsentsFeedbackModel;
import intl.iom.bravemobile.models.activities.DeletionLog;
import intl.iom.bravemobile.models.activities.DistributionAssistance;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.activities.RegistrationActivityModel;
import intl.iom.bravemobile.models.activities.SurveyAnswerModel;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.services.RetrofitService;
import intl.iom.bravemobile.services.SecureStore;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.Extras;
import intl.iom.bravemobile.ui.ConfirmPinPage;
import intl.iom.bravemobile.ui.Dashboard;
import intl.iom.bravemobile.ui.registrations.FillSurveyPage;
import okhttp3.MediaType;
import okhttp3.RequestBody;
import okhttp3.ResponseBody;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;

public class ActivityListPage extends AppCompatActivity {

    private final String TAG = ActivityListPage.class.getSimpleName();

    private ListView lvResults;
    private TextView emptyView;
    private RegistrationActivityAdapter adapter;

    private FloatingActionButton fabDownloadActivity;

    private WithLoading withLoading;
    private Retrofit client;

    RegistrationActivityService registrationActivityService;
    HouseholdRegistrationService householdRegistrationService;
    SecureStore secureStore;

    private static final ExecutorService IO = Executors.newSingleThreadExecutor();

    @Override
    public void onBackPressed() {
        return;
    }


    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_list_page);

        registrationActivityService = ServiceLocator.registrationActivityService(this);
        householdRegistrationService = ServiceLocator.householdRegistrationService(this);
        secureStore = ServiceLocator.secureStore(this);

        BiometricFlow flow = new ClientBiometricFlow();

        flow.init(ActivityListPage.this);

        lvResults = findViewById(R.id.lvResults);
        emptyView = findViewById(R.id.emptyView);
        lvResults.setEmptyView(emptyView);

        fabDownloadActivity = findViewById(R.id.fabDownloadActivity);

        RegistrationActivityValidator registrationActivityValidator = new RegistrationActivityValidator(this);

        setTitle("Field Activity List");

        DialogLoadingHost loading = new DialogLoadingHost(this);
        withLoading = new WithLoading(loading);


        RetrofitService retrofitService = null;
        try
        {
            retrofitService = new RetrofitService(this);
            client = retrofitService.getClient();
        }
        catch (GeneralSecurityException e)
        {
            throw new RuntimeException(e);
        }
        catch (IOException e)
        {
            throw new RuntimeException(e);
        }

        adapter = new RegistrationActivityAdapter(this, registrationActivityService.getAll(), new RegistrationActivityAdapter.OnItemActionListener() {
            @Override
            public void onViewClicked(RegistrationActivity item, int position)
            {
                registrationActivityService.setCurrent(item.code);
                Intent i = new Intent(ActivityListPage.this, ActivityDetailsPage.class);
                //i.putExtra(Extras.EXTRA_ACTIVITY_CODE, item.code);
                startActivity(i);
            }

            @Override
            public void onRefreshClicked(RegistrationActivity item, int position) {
                AlertDialogUtils.confirmDialog(ActivityListPage.this, "Confirm Data Refresh", String.format("Initiating data refresh for activity %s. Proceed with the operation?", item.code), new AlertDialogUtils.Callback() {
                    @Override
                    public void onPositive() {
                        DownloadActivity(item.code);
                    }
                });
            }

            @Override
            public void onSendDataClicked(RegistrationActivity item, int position) {

                // Export public key (PEM) to send to server
                /*String publicPem = null;
                try {
                    publicPem = KeyStoreHelper.getPublicKeyPem();
                } catch (Exception e) {
                    throw new RuntimeException(e);
                }*/

                //Toast.makeText(ActivityListPage.this, publicPem, Toast.LENGTH_SHORT).show();

                AlertDialogUtils.confirmDialog(ActivityListPage.this, "Confirm Data Upload", "Proceeding will upload all collected data for this activity to the server and remove it from your device. Do you want to continue?", new AlertDialogUtils.Callback() {
                    @Override
                    public void onPositive() {


                        if(!registrationActivityValidator.IsActivityDataCollected(item.code))
                        {
                            Toast.makeText(ActivityListPage.this, getString(R.string.activy_data_empty_no_synced), Toast.LENGTH_SHORT).show();
                            return;
                        }

                        boolean ok = RegistrationActivityValidator.all(
                                () -> registrationActivityValidator.AllHouseholdsHaveOnlyOneHead(item.code),
                                () -> registrationActivityValidator.IsAllRequiredSurveysCollected(item.code),
                                () -> registrationActivityValidator.IsAllBiometricsCollected(item.code)
                        );

                        if(!ok)
                        {
                            Toast.makeText(ActivityListPage.this, getString(R.string.activy_data_review_before_synced), Toast.LENGTH_SHORT).show();
                            return;
                        }

                        IO.execute(() -> {

                            withLoading.run("Uploading data..", cb -> {

                                EnrolledApis endpoints = client.create(EnrolledApis.class);

                                try
                                {
                                    // Step 3: Generate nonce
                                    String nonce = NonceUtil.generate();
                                        //UUID.randomUUID().toString();

                                    String timestamp = String.valueOf(System.currentTimeMillis());

                                    /*Response<PublicKeyResponse> res = endpoints.getPubKey().execute();
                                    PublicKeyResponse pub = res.body();

                                    secureStore.setServerPublicKey(pub.publicKeyBase64);*/

                                    AESPacket packet = AESHelper.getPair();

                                    String encryptedKeyBase64 = "aaaaa";//AESHelper.encryptEAS(packet.encKey, secureStore.getServerPublicKey());

                                    byte[] b = NonceUtil.decode(nonce);
                                    byte[] bytes = KeyStoreHelper.sign( b );
                                    String signature = NonceUtil.b64u( bytes );


                                    String headerChunk = "{\"EncryptedKey\":\"" + encryptedKeyBase64 + "\"," +
                                            "\"Nonce\":\"" + nonce + "\"," +
                                            "\"ActivityCode\":\"" + item.code + "\"," +
                                            "\"Timestamp\":\"" + timestamp + "\"," +
                                            "\"DPoP\":\"" + signature + "\"}\n";

                                    StringBuilder ndjsonBuilder = new StringBuilder();
                                    ndjsonBuilder.append(headerChunk);

                                    List<String> hhFromServer = new ArrayList<>();

                                    // Append households
                                    for (Household h : householdRegistrationService.getAll(item.code))
                                    {

                                        if(h.fromServer)
                                        {
                                            hhFromServer.add(h.householdId);
                                        }

                                        //new fields to track theh type of consent
                                        if(h.feedback!=null && !h.fromServer)
                                        {
                                            ConsentsFeedbackModel f = new ConsentsFeedbackModel();
                                            f.consent_id = h.uuid.toString();

                                            h.feedback.type = 1;
                                            h.feedback.consentNotProvided = false;
                                            f.data = ObjectSerializer.serialize(h.feedback);
                                            f.inserted_by = h.createdBy;
                                            f.inserted_on = h.createdOnMs;

                                            //add the consent for household to payload
                                            ndjsonBuilder.append("{\"type\":\"consent\",\"payload\":")
                                                    .append(new Gson().toJson(f))
                                                    .append("}\n");

                                        }

                                        ndjsonBuilder.append("{\"type\":\"household\",\"payload\":")
                                                .append(new Gson().toJson(h))
                                                .append("}\n");
                                    }

                                    // Append individuals
                                    for (Individual i : householdRegistrationService.getIndividualsByActivity(item.code))
                                    {

                                        if(i.feedback!=null && !hhFromServer.contains(i.householdId))
                                        {
                                            ConsentsFeedbackModel f = new ConsentsFeedbackModel();
                                            f.consent_id = i.uuid.toString();

                                            //new fields to track theh type of consent
                                            i.feedback.type = 2;
                                            i.feedback.consentNotProvided = false;

                                            f.data = ObjectSerializer.serialize(i.feedback);

                                            f.inserted_by = i.createdBy;
                                            f.inserted_on = i.createdOnMs;

                                            //add the consent for individual/member to payload
                                            ndjsonBuilder.append("{\"type\":\"consent\",\"payload\":")
                                                    .append(new Gson().toJson(f))
                                                    .append("}\n");
                                        }

                                        ndjsonBuilder.append("{\"type\":\"individual\",\"payload\":")
                                                .append(new Gson().toJson(i))
                                                .append("}\n");
                                    }

                                    // Append consent feedbacks
                                    for (ConsentsFeedbackModel f : householdRegistrationService.getConsentFeedbacksByActivity(item.code)) {
                                        ndjsonBuilder.append("{\"type\":\"consent\",\"payload\":")
                                                .append(new Gson().toJson(f))
                                                .append("}\n");
                                    }

                                    // Append survey answers
                                    for (SurveyAnswerModel f : householdRegistrationService.getSurveyAnswersByActivity(item.code)) {
                                        ndjsonBuilder.append("{\"type\":\"survey\",\"payload\":")
                                                .append(new Gson().toJson(f))
                                                .append("}\n");
                                    }


                                    // Append verifications
                                    for (BiometricCheckModel f : householdRegistrationService.getVerificationsByActivity(item.code)) {
                                        ndjsonBuilder.append("{\"type\":\"verification\",\"payload\":")
                                                .append(new Gson().toJson(f))
                                                .append("}\n");
                                    }

                                    // Append distributions
                                    for (DistributionAssistance d : householdRegistrationService.getAssistancesByActivity(item.code)) {
                                        ndjsonBuilder.append("{\"type\":\"distribution\",\"payload\":")
                                                .append(new Gson().toJson(d))
                                                .append("}\n");
                                    }

                                    // Append deletion logs
                                    for (DeletionLog d : householdRegistrationService.getDeletionLog(item.code)) {
                                        ndjsonBuilder.append("{\"type\":\"del_audit\",\"payload\":")
                                                .append(new Gson().toJson(d))
                                                .append("}\n");
                                    }

                                    /*String encryptedPayload =  AESHelper.encryptPayload(packet.aesKey, packet.iv, ndjsonBuilder.toString());

                                    StringBuilder ndjsonfinal = new StringBuilder();
                                    ndjsonfinal.append(headerChunk);
                                    ndjsonfinal.append(encryptedPayload);

                                    String finalNdjson = ndjsonfinal.toString();*/


                                    String  finalNdjson = ndjsonBuilder.toString();

                                    // Create the RequestBody with x-ndjson media type
                                    RequestBody body = RequestBody.create(
                                            MediaType.parse("application/x-ndjson"),
                                            finalNdjson
                                    );

                                    // Send (asynchronous)
                                    endpoints.postRegistration(body).enqueue(new Callback<ResponseBody>()
                                    {
                                        @Override
                                        public void onResponse(Call<ResponseBody> call, Response<ResponseBody> response)
                                        {
                                            if (response.isSuccessful())
                                            {
                                                // Handle success
                                                cb.onSuccess(null);
                                            }
                                            else
                                            {
                                                // Handle error (e.g., 400/401/500)
                                                cb.onFailure(new Exception(response.message()));
                                            }
                                        }
                                        @Override
                                        public void onFailure(Call<ResponseBody> call, Throwable t) {
                                            // Handle network or serialization error
                                        }
                                    });

                                }
                                catch (Exception e)
                                {
                                    cb.onFailure(e);
                                }



                            }, new WithLoading.ResultHandler<Void>() {
                                @Override
                                public void onSuccess(Void result) {

                                    List<String> subjects = householdRegistrationService.getAllTmpSubjects(item.code);

                                    List<String> households = householdRegistrationService.getEnrolledHouseholds(item.code);

                                    householdRegistrationService.clearActivityData(item.code);

                                    runOnUiThread(() ->
                                            Toast.makeText(ActivityListPage.this, getString(R.string.activy_del_biometric_after_synced_success), Toast.LENGTH_SHORT).show()
                                    );

                                    List<String> sbjs = DataCollectionUtils.filterByPrefixFast(
                                            Arrays.asList(  flow.listIds() ),
                                            households
                                    );

                                    if(sbjs.size()>0)
                                        subjects.addAll(sbjs);

                                    for(String s: subjects)
                                        flow.delete(s);

                                    householdRegistrationService.deleteTmpSubjects(item.code);

                                    runOnUiThread(() ->
                                            Toast.makeText(ActivityListPage.this, getString(R.string.activy_data_synced_success), Toast.LENGTH_SHORT).show()
                                    );

                                }

                                @Override
                                public void onFailure(Throwable t) {

                                    runOnUiThread(() ->
                                            Toast.makeText(ActivityListPage.this, getString(R.string.brave_unknown_error), Toast.LENGTH_SHORT).show()
                                    );

                                }
                            });



                        });



                        //end here
                    }
                });


            }

            @Override
            public boolean isActive(RegistrationActivity item) {
                try {
                    return registrationActivityService.isActive(item.code);
                } catch (RegistrationActivityNotFound e) {
                    Log.e(TAG, e.getMessage());
                    return false;
                }
            }

        });
        lvResults.setAdapter(adapter);

        fabDownloadActivity.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View view) {
                AlertDialogUtils.inputDialog(ActivityListPage.this, "Enter Field Activity Code", "Code provided by PO", value -> {
                            //Toast.makeText(ActivityListPage.this, "Downloading activity: " + value, Toast.LENGTH_SHORT).show();

                    DownloadActivity(value);


                });

            }


        });


    }


    private void DownloadActivity(String value) {
        IO.execute(() -> {

            withLoading.run("Loading..", cb -> {

                EnrolledApis endpoints = client.create(EnrolledApis.class);

                try
                {
                    //Response<Boolean> call = endpoints.basicPing().execute();

                    int tenant = secureStore.getTenantId();

                    String lang  = LanguageHelper.getCurrentLanguage(ActivityListPage.this);

                    Response<RegistrationActivityModel> call = endpoints.getActivity(tenant, value, lang).execute();

                    if(call.isSuccessful()){

                        RegistrationActivityModel result =  call.body();

                        registrationActivityService.saveActivity(result);

                        ServiceLocator.reset(true);

                        runOnUiThread(() -> {

                            Toast.makeText(ActivityListPage.this, "Field Activity details downloaded successfully...", Toast.LENGTH_SHORT).show();
                            adapter.setData(registrationActivityService.getAll());

                        } );


                    }
                    else
                    {
                        runOnUiThread(() ->
                                Toast.makeText(ActivityListPage.this, String.format("Http error: code %d, message :%s",call.code(),call.errorBody()), Toast.LENGTH_SHORT).show()
                        );

                    }

                    cb.onSuccess(null);

                }
                catch (Exception e)
                {

                    runOnUiThread(() ->
                            Toast.makeText(ActivityListPage.this, getString(R.string.brave_network_error), Toast.LENGTH_SHORT).show()
                    );

                    cb.onFailure(e);
                }



            }, new WithLoading.ResultHandler<Void>() {
                @Override
                public void onSuccess(Void result) {

                }

                @Override
                public void onFailure(Throwable t) {

                }
            });



        });
    }
}