package intl.iom.bravemobile.interfaces;

import java.util.List;

import intl.iom.bravemobile.models.activities.BiometricCheckModel;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.Verification;

public interface VerificationService {
    String getJobId(String activity_code);

    default int verificationCount(String activity_code){
        List<Verification> verifications = getAll(activity_code);
        if(verifications==null)return 0;
        return verifications.size();
    }
    List<Verification> getAll(String activity_code);
    BiometricCheckModel getVerification(String uuid);
    void verifyTemplates(String activityCode, String extra, List<BiometricCheckModel> templates, BasicCallback callback);
    Family getMatched(String uuid);
    //void updateVerification(String uuid, boolean is_processed, boolean matched_found, String data);
    void saveVerification(String activity_code, boolean auth, Verification verification);  //auth - for authentication purpose
    List<Verification> getPendingVerification(String code);
    boolean hasPendingVerification(String code);
    String getMatchingId(String code, String householdId);

    void deleteVerification(String code, String uuid, BasicCallback callback);
}
