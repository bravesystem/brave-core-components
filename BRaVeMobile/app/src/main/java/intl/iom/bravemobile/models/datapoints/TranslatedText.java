package intl.iom.bravemobile.models.datapoints;

import com.google.gson.annotations.SerializedName;

public class TranslatedText {
    @SerializedName("language")
    public String lang;
    public String text;
    //public boolean isDefault;

    public TranslatedText(String lang, String text, boolean isDefault) {
        this.lang = lang;
        this.text = text;
       // this.isDefault = isDefault;
    }

    public TranslatedText(String lang, String text) {
        this.lang = lang;
        this.text = text;
    }
}
