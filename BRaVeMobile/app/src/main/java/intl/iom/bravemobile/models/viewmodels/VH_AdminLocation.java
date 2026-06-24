package intl.iom.bravemobile.models.viewmodels;

import android.content.Context;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.adapters.LocationAddressAdapter;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.models.registrations.LocationAddress;
import intl.iom.bravemobile.rules.AdmLevelParser;

public class VH_AdminLocation extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator, CollectionUnitAdapter.DatasetListener {

    private static String TAG = VH_AdminLocation.class.getSimpleName();

    final TextView tvOrder, tvLabel, tvRequired;
    final RecyclerView rvAddressFields;
    final EditText etExtraAddress;
    final LinearLayout llWrapper;

    List<AdminLevel> adminLevels = java.util.Collections.emptyList();

    private Context context;

    public VH_AdminLocation(View v) {
        super(v);
        context = v.getContext();
        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvRequired = v.findViewById(R.id.tvRequired);
        rvAddressFields = v.findViewById(R.id.rvAddressFields);
        rvAddressFields.setLayoutManager(new LinearLayoutManager(context));
        etExtraAddress = v.findViewById(R.id.etExtraAddress);
    }

    boolean isRequired;
    private Map<Integer, String> answers;
    private int selected;
    LocationAddress address;
    LocationAddressAdapter admLevelAdapter;

    int cnt = 0;

    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer, CollectionUnit> questionTypes, Map<Integer, String> answers, CollectionUnitAdapter.AdminLocationProvider adminLocationProvider, CollectionUnit dp, boolean showOrder) {

        viewHolders.put(dp.id, this);

        /*if (vhVisibilityById.get(dp.id))
        {
            show();
        } else {
            hide();
        }*/

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        this.answers = answers;
        this.selected = dp.id;

        isRequired = dp.isRequired;

        if(showOrder)
            tvOrder.setText(String.format("Q.%d",dp.order));
        else
            tvOrder.setVisibility(View.GONE);

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

    public boolean validate()
    {
        return admLevelAdapter.validate();
    }

    @Override
    public void onVisibilityChanged(int id, boolean visible) {

    }

    private boolean override = true;

    @Override
    public void setValidation(String message) {
        tvRequired.setVisibility(isRequired ? View.VISIBLE : View.GONE);
        if (override) {
            tvRequired.setText(message);
        }
        // else ignore and keep existing message
    }

    @Override
    public void setValidation(String message, boolean override) {
        this.override = override;
        tvRequired.setVisibility(isRequired ? View.VISIBLE : View.GONE);

        if (!override) {
            // allowed to update the message
            tvRequired.setText(message);
        }
        // else: just lock, do not override
    }


    @Override
    public void clearValidation() {
        override = true;
        tvRequired.setVisibility(View.GONE);
        tvRequired.setText(null);
    }


    @Override
    public void hide() {
        llWrapper.setVisibility(View.GONE);
    }

    @Override
    public void show() {
        llWrapper.setVisibility(View.VISIBLE);
    }


    @Override
    public void submitAll() {

        admLevelAdapter.clearAnyFocus(rvAddressFields);
        address.admLocations = admLevelAdapter.getLocations();

        if (!validateRow(address)) {
            //Toast.makeText(this, "Please complete required fields.", Toast.LENGTH_SHORT).show();
            answers.put(selected,  null);
            return;
        }

        answers.put(selected, ObjectSerializer.serialize(address));

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
}
