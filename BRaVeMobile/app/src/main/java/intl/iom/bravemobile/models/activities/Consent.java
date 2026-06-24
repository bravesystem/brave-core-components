package intl.iom.bravemobile.models.activities;

import java.util.List;

import intl.iom.bravemobile.helpers.TranslationUtils;
import intl.iom.bravemobile.models.datapoints.TranslatedText;
import intl.iom.bravemobile.statics.ConsentType;

public class Consent {

    public int id;
    public int order;
    public boolean isRequired;
    public ConsentType type;
    public List<TranslatedText> translations; // will no longer use it

    private String title;
    private String description;
    private boolean is_active;

    public Consent(){}

    public Consent(ConsentModel restored, int order, int type, boolean isRequired) {

        id = restored.id;
        title = restored.title;
        description = restored.description;
        is_active = restored.is_active;

        this.order = order;
        this.type = ConsentType.fromCode(type);
        this.isRequired = isRequired;

    }


    public String getDefaultText()
    {
        return description;
        //return TranslationUtils.get(this.translations, "en");
    }

    public String getTitle() {return title;}
}
