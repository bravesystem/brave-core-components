package intl.iom.bravemobile.rules;

import com.google.gson.annotations.SerializedName;

public class RestrictionConfig {


    public Double min;
    public Double max;
    public Boolean allowNegative;
    @SerializedName("default")
    public String defaultValue;

    public Boolean readonly;
    public String pattern;
    public String errorMessage;


}
