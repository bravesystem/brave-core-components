package intl.iom.bravemobile.models.activities;

import java.util.List;

import intl.iom.bravemobile.models.datapoints.TranslatedText;

public class DatapointModel {

    public int id;
    public int order ;
    public String defaultLanguage ;
    public boolean isRequired ;
    public int type ;
    public Integer lookup ;
    public Integer dataset;
    public Integer minVal ;
    public Integer maxVal ;
    public String restriction ;
    public List<TranslatedText> texts ;
}
