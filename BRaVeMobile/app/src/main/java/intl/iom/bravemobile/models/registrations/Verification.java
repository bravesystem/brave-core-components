package intl.iom.bravemobile.models.registrations;

import android.content.ContentValues;

public class Verification {

    public String uuid;
    public int gender; // 1 = Male, 2 = Female (adjust as needed)
    public String template;
    public long inserted_on; // epoch seconds or milliseconds
    public Long updated_on; // epoch seconds or milliseconds
    public String inserted_by; // epoch seconds or milliseconds
    public boolean is_processed;
    public boolean matched_found;
    public String matched_uuid; // nullable
    public String data; // extra info

    //for testing purpose only
    public Verification(String uuid, int gender, long inserted_on,
                        boolean is_processed, boolean matched_found,
                        String matched_uuid, String data) {
        this.uuid = uuid;
        this.gender = gender;
        this.inserted_on = inserted_on;
        this.is_processed = is_processed;
        this.matched_found = matched_found;
        this.matched_uuid = matched_uuid;
        this.data = data;
    }

    public Verification(String uuid, int gender, String template, long inserted_on, long updated_on, String inserted_by,
                        boolean is_processed, boolean matched_found,
                        String matched_uuid, String data)
    {
        this.uuid = uuid;
        this.gender = gender;
        this.template = template;
        this.inserted_on = inserted_on;
        this.updated_on = updated_on;
        this.inserted_by = inserted_by;
        this.is_processed = is_processed;
        this.matched_found = matched_found;
        this.matched_uuid = matched_uuid;
        this.data = data;
    }

    public Verification(String uuid, int gender, String template)
    {
        this.uuid = uuid;
        this.gender = gender;
        this.template = template;
    }

    public ContentValues getInsertValues(String enumerator, long current)
    {
        ContentValues cv = new ContentValues();

        cv.put("uuid", uuid);
        cv.put("gender", gender);
        cv.put("template", template);
        cv.put("is_processed", 0);
        cv.put("matched_found", 0);
        cv.put("inserted_by", enumerator);
        cv.put("inserted_on", current);

        return cv;
    }

}
