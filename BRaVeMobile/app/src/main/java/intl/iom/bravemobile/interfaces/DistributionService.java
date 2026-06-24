package intl.iom.bravemobile.interfaces;

import org.json.JSONObject;

import java.util.List;

import intl.iom.bravemobile.models.distributions.DistDto;
import intl.iom.bravemobile.models.distributions.DistItem;
import intl.iom.bravemobile.models.distributions.Distribution;
import intl.iom.bravemobile.models.distributions.EnrollmentResponse;
import intl.iom.bravemobile.models.distributions.Family;

public interface DistributionService {

    void setCurrent(int distribution_id);
    int getCurrent();

    DistDto getCurItems();
    void setCurItems(DistDto dto);

    List<Distribution> getAll(String activity_id);

    void enrollMatch(String activityCode, int distributionId, String uuid, Family beneficiary);

    boolean isEnrolled(String activityCode,int distribution, String beneficiaryId);

    Family getEnrolledBeneficiary(String activityCode, int distribution, String beneficiaryId);
    void checkEnrollment(String activityCode, int distribution, String beneficiaryId, EnrollmentCallback callback);

    int assistanceCount(String activity_code);

    boolean saveEnrollment(String activityCode, int distributionId, String beneficiaryId, Family family);

    boolean saveAssistance(String activityCode, int distribution, String beneficiaryId, int individualId, String data);

    void fetchEnrollmentData(String activityCode, int distributionId, String beneficiaryId, BasicCallback basicCallback);

    int getTotalEnrollments(String activityCode, int distributionId);

    boolean mapHousehold(String code,int distributionId, String uuid, Family restored);
}
