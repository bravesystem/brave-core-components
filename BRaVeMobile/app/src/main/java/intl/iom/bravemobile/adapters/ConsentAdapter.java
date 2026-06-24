package intl.iom.bravemobile.adapters;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.activities.Consent;
import intl.iom.bravemobile.models.datapoints.RegistrationPreference;
import intl.iom.bravemobile.statics.ConsentType;
import intl.iom.bravemobile.statics.DataPointType;


public class ConsentAdapter extends RecyclerView.Adapter<ConsentAdapter.VH> {

    private final List<Consent> items;

    public ConsentAdapter(List<Consent> items) {
        this.items = items;
    }

    @Override public VH onCreateViewHolder(ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_consent, parent, false);
        return new VH(v);
    }

    @Override
    public void onBindViewHolder(VH h, int position) {
        Consent consent = items.get(position);
        
        h.tvConsentText.setText(StringUtils.truncateWithDots(consent.getDefaultText()));
        h.tvConsentCategory.setText(consent.type.getLabel());

        int typeColor = (consent.type == ConsentType.INDIVIDUAL.INDIVIDUAL) ? 0xFF1976D2 : 0xFF00796B; // blue vs teal
        h.tvConsentCategory.getBackground().setTint(typeColor);

        if (consent.isRequired) {
            h.tvConsentRequired.setText("Required");

            // ✅ Tint background green for required
            h.tvConsentRequired.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(h.itemView.getContext(), R.color.success_green)
            );


        } else {
            h.tvConsentRequired.setText("Optional");

            // ✅ Tint background teal for optional
            h.tvConsentRequired.getBackground().setTint(
                    androidx.core.content.ContextCompat.getColor(h.itemView.getContext(), R.color.info_teal)
            );

        }
    }


    @Override public int getItemCount() { return items.size(); }

    static class VH extends RecyclerView.ViewHolder {
        final TextView tvConsentText, tvConsentCategory, tvConsentRequired;
        VH(View v) {
            super(v);
            tvConsentText = v.findViewById(R.id.tvConsentText);
            tvConsentCategory = v.findViewById(R.id.tvConsentCategory);
            tvConsentRequired = v.findViewById(R.id.tvConsentRequired);
        }
    }
}

