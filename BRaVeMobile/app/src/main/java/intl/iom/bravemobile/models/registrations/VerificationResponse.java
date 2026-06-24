package intl.iom.bravemobile.models.registrations;

import intl.iom.bravemobile.models.distributions.Family;

public class VerificationResponse {

    public String uuid;
    public String matched_uuid;
    public boolean is_processed;
    public boolean match_found;
    public int score;
    public int is_enrolled = 0;
    public Family matched;
}
