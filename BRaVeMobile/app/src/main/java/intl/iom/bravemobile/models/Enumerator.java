package intl.iom.bravemobile.models;

import android.content.ContentValues;
import android.util.Base64;

import com.google.gson.annotations.SerializedName;

import intl.iom.bravemobile.helpers.DateUtils;

public class Enumerator {
    @SerializedName("enumeratorCode")
    public String code;
    public String fullName;
    public String note;
    public String type; //S -supervisor, E- enumerator
    public String pin;
    public String pinB64;
    public String photoBase64;
    public byte[] enumeratorPin;
    public boolean isActive;
    public boolean isSupervisor;
    public boolean isPinUpdated;
    public long lastUpdated;
    public long expiredAt;

    public boolean isPinChangeRequired()
    {
        if(!isPinUpdated)
            return true;

        if(DateUtils.isExpired(expiredAt))
            return true;

        return false;
    }

    public boolean pinChangeRequired()
    {
        return enumeratorPin==null || enumeratorPin.length==0 ;
    }

    public ContentValues getContentValues()
    {
        ContentValues cv = new ContentValues();

        cv.put("code", code.toUpperCase());
        cv.put("fullName", fullName);
        cv.put("photoBase64", photoBase64);
        cv.put("note", note);
        cv.put("isSupervisor", isSupervisor?1:0);
        cv.put("isActive", isActive?1:0);
        cv.put("isPinUpdated", isPinUpdated?1:0);
        if(pinB64!=null){
            cv.put("enumeratorPin",  Base64.decode(pinB64, Base64.NO_WRAP) );
        }
        cv.put("UpdatedOnMs", lastUpdated);

        return cv;
    }
}
