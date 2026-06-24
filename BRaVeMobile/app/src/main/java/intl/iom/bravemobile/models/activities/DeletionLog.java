package intl.iom.bravemobile.models.activities;

import android.database.Cursor;

public class DeletionLog {

    private long id;
    private String activityCode;
    private String deletionType;       // "household" or "individual"
    private String householdId;
    private Integer individualId;     // null for household deletions
    private String deletedIndividualIds; // JSON array of logical IDs
    private String justification; // JSON array of logical IDs
    private long deletedAt;            // Unix ms
    private String deletedBy;          // optional user/enumerator code

    public DeletionLog() {
    }

    public DeletionLog(long id, String activityCode, String deletionType, String householdId, Integer individualId,
                       String deletedIndividualIds, String justification, long deletedAt, String deletedBy) {
        this.id = id;
        this.activityCode = activityCode;
        this.deletionType = deletionType;
        this.householdId = householdId;
        this.individualId = individualId;
        this.deletedIndividualIds = deletedIndividualIds;
        this.justification = justification;
        this.deletedAt = deletedAt;
        this.deletedBy = deletedBy;
    }

    /** Create a DeletionLog from a Cursor (e.g. after querying tbl_deleted_records). */

    //    public static final String SQL_CREATE_TABLE =
    //    "CREATE TABLE IF NOT EXISTS tbl_deleted_records(id INTEGER PRIMARY KEY AUTOINCREMENT,
    //    activity_code TEXT, deletion_type TEXT NOT NULL, household_id TEXT NOT NULL,
    //    individual_id INT, deleted_individual_ids TEXT NOT NULL,
    //    justification TEXT, deleted_at INT NOT NULL, deleted_by TEXT);";
    public static DeletionLog fromCursor(Cursor c) {

        DeletionLog log = new DeletionLog();
        log.id = c.getLong(c.getColumnIndexOrThrow("id"));
        int idxActivity = c.getColumnIndex("activity_code");
        log.activityCode =  c.getString(idxActivity);
        log.deletionType = c.getString(c.getColumnIndexOrThrow("deletion_type"));
        log.householdId= c.getString(c.getColumnIndexOrThrow("household_id"));
        int idxInd = c.getColumnIndex("individual_id");
        log.individualId = idxInd >= 0 && !c.isNull(idxInd) ? c.getInt(idxInd) : null;
        log.deletedIndividualIds = c.getString(c.getColumnIndexOrThrow("deleted_individual_ids"));
        int idxJust = c.getColumnIndex("justification");
        log.justification = c.getString(idxJust);
        log.deletedAt = c.getLong(c.getColumnIndexOrThrow("deleted_at"));
        int idxBy = c.getColumnIndex("deleted_by");
        log.deletedBy =  c.getString(idxBy);

        return log;
    }
}
