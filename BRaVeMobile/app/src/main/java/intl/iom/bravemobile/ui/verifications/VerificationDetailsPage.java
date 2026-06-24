package intl.iom.bravemobile.ui.verifications;

import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import android.content.Intent;
import android.os.Bundle;
import android.view.View;
import android.widget.TextView;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.VerifMemberAdapter;
import intl.iom.bravemobile.interfaces.VerificationService;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.IntentKeys;

public class VerificationDetailsPage extends AppCompatActivity {

    private RecyclerView rvMembers;
    private TextView tvEmpty;
    private VerifMemberAdapter adapter;

    @Override
    public void onBackPressed() {
        return;
    }
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_verification_details_page);

        setTitle("Match Found..");

        VerificationService  verificationService = ServiceLocator.verificationService(this);

        rvMembers = findViewById(R.id.rvMembers);
        tvEmpty = findViewById(R.id.tvEmpty);

        rvMembers.setLayoutManager(new LinearLayoutManager(this));

        String uuid = getIntent().getStringExtra(IntentKeys.HOUSEHOLD_ID);

        Family family =
            verificationService.getMatched(uuid);

        // Example data
        List<Family.Member> members = family.getMembers();
        String highlightUuid = family.getMatchedUuid();

        adapter = new VerifMemberAdapter(members, highlightUuid);
        rvMembers.setAdapter(adapter);

        toggleEmptyState(members);
    }

    /*private List<Family.Member> loadMembers() {
        String familyJson = "{\n" +
                "  \"HouseholdUuid\": \"xxx\",\n" +
                "  \"hohid\": \"aaa\",\n" +
                "  \"type\": \"refugee\",\n" +
                "  \"members\": [\n" +
                "    {\n" +
                "      \"uuid\": \"yyy\",\n" +
                "      \"memno\": 1,\n" +
                "      \"relationship\": \"head\",\n" +
                "      \"full_name\": \"bla bla\",\n" +
                "      \"gender\": \"male\",\n" +
                "      \"age\": 23,\n" +
                "      \"photoB64\": \"\",\n" +
                "      \"biometricB64\": \"****\"\n" +
                "    },\n" +
                "    {\n" +
                "      \"uuid\": \"zzz\",\n" +
                "      \"memno\": 2,\n" +
                "      \"relationship\": \"spouse\",\n" +
                "      \"full_name\": \"bla bla\",\n" +
                "      \"gender\": \"female\",\n" +
                "      \"age\": 23,\n" +
                "      \"photo\": \"\",\n" +
                "      \"biometric\": \"****\"\n" +
                "    }\n" +
                "  ]\n" +
                "}";

        Family family = parseFamilyFromJson(familyJson);

        return family.getMembers();
    }
*/

    private Family parseFamilyFromJson(String json) {
        try {
            JSONObject o = new JSONObject(json);

            // Support both "HouseholdUuid" and "householdUuid"
            String householdUuid = o.optString("HouseholdUuid", o.optString("householdUuid"));
            String hohid = o.optString("hohid");
            String type = o.optString("type");

            JSONArray membersJson = o.getJSONArray("members");
            ArrayList<Family.Member> members = new ArrayList<>();
            for (int i = 0; i < membersJson.length(); i++) {
                JSONObject m = membersJson.getJSONObject(i);
                String uuid = m.optString("uuid");
                int memno = m.optInt("memno");
                String relationship = m.optString("relationship");
                String fullName = m.optString("full_name");
                String gender = m.optString("gender");
                int age = m.optInt("age");

                String photo = m.optString("photo");
                String biometric = m.optString("biometric");

                members.add(new Family.Member(uuid, memno, relationship, fullName, gender, age,  photo, biometric));
            }

            return new Family(householdUuid, hohid, type, members);

        } catch (JSONException e) {
            throw new IllegalArgumentException("Invalid family JSON: " + e.getMessage(), e);
        }
    }

    private void toggleEmptyState(List<Family.Member> members) {
        boolean isEmpty = members == null || members.isEmpty();
        tvEmpty.setVisibility(isEmpty ? View.VISIBLE : View.GONE);
        rvMembers.setVisibility(isEmpty ? View.GONE : View.VISIBLE);
    }
}