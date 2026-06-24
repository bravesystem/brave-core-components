package intl.iom.bravemobile.models.updatedviewmodels;

import android.content.Context;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.LocationAddressAdapter;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.AdminLocationProvider;
import intl.iom.bravemobile.interfaces.PreValidateAction;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.LocationAddress;
import intl.iom.bravemobile.rules.AdmLevelParser;

public class VH_AdminLocation_Updated extends LinearLayout implements ViewValidator, PreValidateAction {

    private static String TAG = VH_AdminLocation_Updated.class.getSimpleName();

    final SurveyDataContext surveyDataContext;
    final TextView tvOrder, tvLabel, tvRequired;
    final RecyclerView rvAddressFields;
    final EditText etExtraAddress;
    final LinearLayout llWrapper;
    final Context context;
    final  int questionId;
    List<AdminLevel> adminLevels = java.util.Collections.emptyList();
    LocationAddress address;
    LocationAddressAdapter admLevelAdapter;


    public VH_AdminLocation_Updated(int questionId, Context context, SurveyDataContext surveyDataContext ) {

        super(context);

        this.questionId = questionId;

        this.context = context;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_admin_location,  this, true);

        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvRequired = v.findViewById(R.id.tvRequired);
        rvAddressFields = v.findViewById(R.id.rvAddressFields);
        rvAddressFields.setLayoutManager(new LinearLayoutManager(context));
        etExtraAddress = v.findViewById(R.id.etExtraAddress);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }

    int cnt = 0;

    public void bind()
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;
        AdminLocationProvider adminLocationProvider = surveyDataContext.adminLocationProvider;

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());

        int LevelId = 0;

        try {
            LevelId = AdmLevelParser.parseAdmLevelId(dp.restriction);
        } catch (Exception e) {
            Log.e(TAG, e.getMessage());
        }

        // 1) Load Admin Levels
        adminLevels = adminLocationProvider != null
                ? adminLocationProvider.getAdminLevelsUpTo(LevelId)
                : java.util.Collections.emptyList();

        cnt = adminLevels.size();

        address = new LocationAddress();

        // 4) Restore selection from saved answer
        String existing = answers.get(dp.id);

        if(!StringUtils.isBlank(existing))
            address = ObjectSerializer.deserialize(existing, LocationAddress.class);

        etExtraAddress.setText(address.siteAddress);

        admLevelAdapter = new LocationAddressAdapter(LayoutInflater.from(context), new LocationAddressAdapter.OptionsProvider() {
            @Override
            public List<SelectItem> getOptionsFor(String lookupName) {
                return null;
            }

            @Override
            public List<SelectItem> getOptionsFor(int level, Integer parent) {

                if(level>1 && parent==null)
                    return new ArrayList<>();

                if(cnt>0)
                {
                    cnt = cnt -1;
                }
                else
                {
                    for(int i=level; i<=adminLevels.size(); i++)
                        admLevelAdapter.getLocations().put(i, null);
                }

                return adminLocationProvider.getOptionsFor( parent);

            }
        }, new LocationAddressAdapter.AnswerListener() {
            @Override
            public void onAnswerChanged(AdminLevel level, String value) {

            }
        }, adminLevels, address.admLocations);

        rvAddressFields.setAdapter(admLevelAdapter);

        etExtraAddress.setOnFocusChangeListener((view, hasFocus) -> {
            if (!hasFocus) {
                String val = etExtraAddress.getText().toString();
                address.siteAddress = val;
                //answers.put(dp.id, ObjectSerializer.serialize(address));
            }
        });

    }

    @Override
    public void setValidation(String message) {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        tvRequired.setVisibility( dp.isRequired ? View.VISIBLE : View.GONE);
        tvRequired.setText(message);
    }

    @Override
    public void clearValidation() {
        tvRequired.setVisibility(View.GONE);
        tvRequired.setText(null);
    }

    @Override
    public void submitAll() {

        Map<Integer, String> answers = surveyDataContext.answers;

        admLevelAdapter.clearAnyFocus(rvAddressFields);
        address.admLocations = admLevelAdapter.getLocations();

        if (!validateRow(address)) {
            //Toast.makeText(this, "Please complete required fields.", Toast.LENGTH_SHORT).show();
            answers.put(questionId,  null);
            return;
        }

        answers.put(questionId, ObjectSerializer.serialize(address));

        //show or hide here
        surveyDataContext.bubbleDown(questionId, surveyDataContext.questions.get(questionId).order);

    }

    @Override
    public boolean validate() {
        return admLevelAdapter.validate();
    }

    private boolean validateRow(LocationAddress address)
    {

        for ( AdminLevel a: adminLevels  )
        {
            if(a.isRequired )
            {
                if(!address.admLocations.containsKey(a.id))
                    return false;

                if(address.admLocations.get(a.id)==null)
                    return false;
            }
        }

        return true;
    }

    /*public boolean validate()
    {
        return admLevelAdapter.validate();
    }*/

}
