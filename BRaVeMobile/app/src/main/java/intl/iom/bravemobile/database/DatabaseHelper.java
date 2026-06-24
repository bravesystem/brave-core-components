package intl.iom.bravemobile.database;

import android.content.Context;
import android.database.sqlite.SQLiteDatabase;
import android.database.sqlite.SQLiteOpenHelper;

public class DatabaseHelper extends SQLiteOpenHelper {

    private static final String DB_NAME = "brave.db";
    private static final int DB_VERSION = 15;

    public DatabaseHelper(Context context) { super(context, DB_NAME, null, DB_VERSION); }

    @Override
    public void onCreate(SQLiteDatabase db) {

        //inserted_at_utc INTEGER NOT NULL DEFAULT (strftime('%s','now') * 1000)

        String query = "CREATE TABLE IF NOT EXISTS tbl_jwt_tokens(environment TEXT primary key, jwt TEXT, refresh TEXT, updated_on INT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_current_household_id(environment TEXT primary key, current_id INT, updated_on TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_enumerators ( code TEXT PRIMARY KEY, fullName TEXT NOT NULL, photoBase64 TEXT, note TEXT,  isSupervisor INTEGER, isActive INTEGER NOT NULL DEFAULT 1, isPinUpdated INTEGER NOT NULL DEFAULT 0, enumeratorPin BLOB, UpdatedOnMs  INTEGER NOT NULL);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_admin_levels(id INT primary key, name TEXT, is_active INT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_admin_locations(id INT primary key, name TEXT, level INT, parent INT, is_active INT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_lookup_names(id INT primary key, name TEXT, is_active INT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_lookup_values(id INT, lookup_id INT, name TEXT, is_active INT, primary key(id, lookup_id));";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_datasets(id INT primary key, data TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_registration_activities(id TEXT primary key, data TEXT, updated_on INT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_consents(id INT primary key, data TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_surveys(id INT primary key, data TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_survey_answers(activity_code TEXT, id INT,household_id TEXT, individual_id INT, data TEXT, primary key(activity_code, id, household_id, individual_id));";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_consent_feedbacks(activity_code TEXT, consent_id TEXT primary key, data TEXT, inserted_on INT, inserted_by TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_datapoints(id INT primary key, data TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_preferences(activity_code TEXT, id INT, name TEXT, description TEXT, type INT, value TEXT,  primary key(activity_code,id));";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_households(activity_code TEXT, household_id TEXT primary key, individual_no INT, from_server INT, read_only INT, data TEXT, inserted_on INT, inserted_by TEXT, updated_on INT, updated_by TEXT);";
        db.execSQL(query);

        //deleted -0 means active, deleted -1 means flagged as deleted
        query = "CREATE TABLE IF NOT EXISTS tbl_individuals(activity_code TEXT, household_id TEXT, individual_id INT, deleted INT, data TEXT, inserted_on INT, inserted_by TEXT, updated_on INT, updated_by TEXT, primary key(household_id, individual_id) );";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_verifications(activity_code TEXT, uuid TEXT primary key, auth INT, gender INT, template TEXT,  is_processed INT, matched_found INT, matched_uuid TEXT, score INT, inserted_by TEXT, inserted_on INT, updated_on INT, data TEXT);";
        db.execSQL(query);   //auth:  NULL or 0 - for normal verification, 1 - for live authentication

        query = "CREATE TABLE IF NOT EXISTS tbl_distributions(id INT primary key,data TEXT);";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_enrolled_beneficiaries(activity_code TEXT, distribution_id INT, household_id TEXT, individual_id INT, data TEXT, primary key(activity_code, distribution_id, household_id, individual_id));";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_distribution_assistances(activity_code TEXT, id INT, household_id TEXT, individual_id INT, data TEXT, primary key(activity_code, id, household_id, individual_id));";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_tmp_biometrics(activity_code TEXT, uuid TEXT, primary key(uuid) );";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_tmp_verification_requests(activity_code TEXT, jobid TEXT, primary key(activity_code) );";
        db.execSQL(query);

        query = "CREATE TABLE IF NOT EXISTS tbl_deleted_records(id INTEGER PRIMARY KEY AUTOINCREMENT, activity_code TEXT, deletion_type TEXT NOT NULL, household_id TEXT NOT NULL, individual_id INT, deleted_individual_ids TEXT NOT NULL, justification TEXT, deleted_at INT NOT NULL, deleted_by TEXT);";
        db.execSQL(query);


        query = "CREATE TABLE IF NOT EXISTS tbl_distr_verif_mapping( activity_code TEXT, uuid TEXT primary key, household_id TEXT NOT NULL, inserted_on INT NOT NULL);";
        db.execSQL(query);

    }

    @Override
    public void onUpgrade(SQLiteDatabase db, int oldVersion, int newVersion) {

        if (newVersion <= oldVersion) return;


        db.beginTransaction();
        try {
            // Example: drop the table when upgrading to version 3 or higher
            // Adjust the condition to fit your migration plan.
            if (oldVersion < 3) {
                db.execSQL("DROP TABLE IF EXISTS tbl_survey_answers");

                String query = "CREATE TABLE IF NOT EXISTS tbl_survey_answers(activity_code TEXT, id INT,household_id TEXT, individual_id INT, data TEXT, primary key(activity_code, id, household_id, individual_id));";
                db.execSQL(query);
            }

            if (oldVersion <= 6) {
                db.execSQL("DROP TABLE IF EXISTS tbl_tmp_biometrics");

                String query = "CREATE TABLE IF NOT EXISTS tbl_tmp_biometrics( activity_code TEXT, uuid TEXT, primary key(uuid) );";
                db.execSQL(query);
            }

            if (oldVersion <= 7) {
                db.execSQL("DROP TABLE IF EXISTS tbl_tmp_verification_requests");

                String query = "CREATE TABLE IF NOT EXISTS tbl_tmp_verification_requests( activity_code TEXT, jobid TEXT, primary key(activity_code) );";
                db.execSQL(query);
            }

            db.execSQL("DROP TABLE IF EXISTS tbl_verifications");

            String query = "CREATE TABLE IF NOT EXISTS tbl_verifications(activity_code TEXT, uuid TEXT primary key, gender INT, template TEXT, is_processed INT, matched_found INT, matched_uuid TEXT, score INT, inserted_by TEXT, inserted_on INT, updated_on INT,  data TEXT);";
            db.execSQL(query);

            if (oldVersion <= 10) {
                db.execSQL("DROP TABLE IF EXISTS tbl_distributions");

                query = "CREATE TABLE IF NOT EXISTS tbl_distributions(id INT primary key, data TEXT);";
                db.execSQL(query);
            }

            db.execSQL("DROP TABLE IF EXISTS tbl_registration_activities");

            query = "CREATE TABLE IF NOT EXISTS tbl_registration_activities(id TEXT primary key, data TEXT, updated_on INT);";
            db.execSQL(query);

            db.execSQL("DROP TABLE IF EXISTS tbl_enrolled_beneficiaries");

            query = "CREATE TABLE IF NOT EXISTS tbl_enrolled_beneficiaries(activity_code TEXT, distribution_id INT, household_id TEXT, individual_id INT, data TEXT, primary key(activity_code, distribution_id, household_id, individual_id));";
            db.execSQL(query);

            db.execSQL("DROP TABLE IF EXISTS tbl_distribution_assistances");

            query = "CREATE TABLE IF NOT EXISTS tbl_distribution_assistances(activity_code TEXT, id INT, household_id TEXT, individual_id INT, data TEXT, primary key(activity_code, id, household_id, individual_id));";
            db.execSQL(query);

            query = "CREATE TABLE IF NOT EXISTS tbl_deleted_records(id INTEGER PRIMARY KEY AUTOINCREMENT, activity_code TEXT, deletion_type TEXT NOT NULL, household_id TEXT NOT NULL, individual_id INT, deleted_individual_ids TEXT NOT NULL, justification TEXT, deleted_at INT NOT NULL, deleted_by TEXT);";
            db.execSQL(query);

            db.execSQL("DROP TABLE IF EXISTS tbl_verifications");

            query = "CREATE TABLE IF NOT EXISTS tbl_verifications(activity_code TEXT, uuid TEXT primary key, auth INT, gender INT, template TEXT,  is_processed INT, matched_found INT, matched_uuid TEXT, score INT, inserted_by TEXT, inserted_on INT, updated_on INT, data TEXT);";
            db.execSQL(query);   //auth:  NULL or 0 - for normal verification, 1 - for live authentication

            db.execSQL("DROP TABLE IF EXISTS tbl_distr_verif_mapping");

            query = "CREATE TABLE IF NOT EXISTS tbl_distr_verif_mapping( activity_code TEXT, uuid TEXT primary key, household_id TEXT NOT NULL, inserted_on INT NOT NULL);";
            db.execSQL(query);

            db.setTransactionSuccessful();
        } finally {
            db.endTransaction();
        }

    }
}
