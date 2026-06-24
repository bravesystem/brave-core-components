package intl.iom.bravemobile.models.datapoints;

import intl.iom.bravemobile.exceptions.RegistrationActivityNotFound;
import intl.iom.bravemobile.exceptions.RegistrationPreferenceError;
import intl.iom.bravemobile.statics.AnswerType;

public class RegistrationPreference {
    public String name;
    public AnswerType type;
    public String value;

    public RegistrationPreference(String name, AnswerType type, String value) throws RegistrationPreferenceError
    {
        this.name = name;
        this.type = type;
        this.value = value;

        if(type!=AnswerType.BOOLEAN && type!=AnswerType.TEXT && type!=AnswerType.NUMERIC && type!=AnswerType.INT)
            throw new RegistrationPreferenceError("Invalid preference type. Allowed types: boolean, text, integer, or numeric.");
    }
}
