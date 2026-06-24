package intl.iom.bravemobile.models.activities;

import java.io.Serializable;
import java.util.LinkedHashMap;

public class ConsentsFeedback implements Serializable {

    public LinkedHashMap<Integer, Boolean> answers;
    public String comment; //justification

    public int type = 0;
    public boolean consentNotProvided = true;
}
