package intl.iom.bravemobile.adapters;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.core.content.ContextCompat;
import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;

public class PreferenceAdapter extends RecyclerView.Adapter<PreferenceAdapter.VH> {

    private final List<RegistrationPreference> items;

    public PreferenceAdapter(List<RegistrationPreference> items) {
        this.items = items;
    }

    @Override
    public VH onCreateViewHolder(ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_registration_preference, parent, false);
        return new VH(v);
    }

    @Override
    public void onBindViewHolder(VH h, int position) {

            RegistrationPreference pref = items.get(position);
            h.tvPreference.setText(pref.name);
            h.tvPreferenceType.setText(pref.type.getLabel());
            h.tvPreferenceValue.setText(pref.value);

            // ✅ Dynamic color tint based on value/type
            String value = pref.value != null ? pref.value.trim().toLowerCase() : "";
            int colorRes;

            if (value.equals("true")) {
                colorRes = R.color.success_green;   // 🟢 true
            } else if (value.equals("false")) {
                colorRes = R.color.error_red;       // 🔴 false
            } else if (value.matches("\\d+")) {
                colorRes = R.color.brave_blue;      // 🔵 numeric
            } else if (value.equals("optional")) {
                colorRes = R.color.info_teal;       // 🟦 optional
            } else {
                colorRes = R.color.neutral_grey;    // ⚪ default
            }

            h.tvPreferenceValue.getBackground().setTint(
                    ContextCompat.getColor(h.itemView.getContext(), colorRes)
            );



    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class VH extends RecyclerView.ViewHolder {
        final TextView tvPreference, tvPreferenceType, tvPreferenceValue;

        VH(View v) {
            super(v);
            tvPreference = v.findViewById(R.id.tvPreference);
            tvPreferenceType = v.findViewById(R.id.tvPreferenceType);
            tvPreferenceValue = v.findViewById(R.id.tvPreferenceValue);
        }
    }
}
