package intl.iom.bravemobile.helpers;


import android.content.Context;
import android.util.Log;
import android.widget.Toast;

import com.google.android.gms.tasks.OnSuccessListener;
import com.google.android.gms.tasks.OnFailureListener;
import com.google.android.play.core.integrity.IntegrityManager;
import com.google.android.play.core.integrity.IntegrityManagerFactory;
import com.google.android.play.core.integrity.IntegrityTokenRequest;
import com.google.android.play.core.integrity.IntegrityTokenResponse;

public class IntegrityHelper {

    public static long Project_Number = 952318751944L;

    public interface RequestCallback{
        void sendTokenToBackend(IntegrityTokenResponse integrityTokenResponse);
    }

    public static String TAG = IntegrityHelper.class.getSimpleName();

    private Context context;

    public IntegrityHelper(Context context) {
        this.context = context;
    }

    public void requestIntegrityToken(String nonce, long cloudProjectNumber, RequestCallback callback) {
        IntegrityManager integrityManager = IntegrityManagerFactory.create(context);

        IntegrityTokenRequest request = IntegrityTokenRequest.builder()
                .setNonce(nonce)
                .setCloudProjectNumber(cloudProjectNumber)
                .build();

        integrityManager.requestIntegrityToken(request)
                .addOnSuccessListener(integrityTokenResponse -> {
                    //Call your callback with the token response
                    callback.sendTokenToBackend(integrityTokenResponse);
                })
                .addOnFailureListener(e -> {
                    //Handle failure gracefully
                    Log.e("IntegrityHelper", "Error requesting integrity token: " + e.getMessage());
                    Toast.makeText(context, "Failed to verify device integrity. Please try again.", Toast.LENGTH_LONG).show();
                });
    }

}