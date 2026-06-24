package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.PorterDuff;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;

import java.text.DateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.List;
import java.util.Locale;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.interfaces.HouseholdRegistrationService;
import intl.iom.bravemobile.models.CustomValidationResponse;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.services.ServiceLocator;
import intl.iom.bravemobile.statics.KnownLkp;
import intl.iom.bravemobile.statics.ReservedLookups;

public class IndividualAdapter extends BaseAdapter {

    public interface OnItemActionListener
    {
        /** e.g., getLookupItemLabel(1, "gender") -> "Male" */
        String getLookupItemLabel(int id, int domain);
        void onEditClicked(Individual item, int position);
        void onDeleteClicked(Individual item, int position);
        CustomValidationResponse isValid(Individual item);
    }

    private final LayoutInflater inflater;
    private final DateFormat dateFmt;
    private final List<Individual> items = new ArrayList<>();
    private final IndividualAdapter.OnItemActionListener listener;

    private HouseholdRegistrationService householdRegistrationService;

    private final int photoPlaceholderRes; // e.g., R.drawable.ic_no_photo

    private int hhcount;
    private boolean first = false;

    public IndividualAdapter(Context context,
                             List<Individual> initial,
                             IndividualAdapter.OnItemActionListener listener,
                             int photoPlaceholderRes) {

        householdRegistrationService = ServiceLocator.householdRegistrationService(context);
        this.inflater = LayoutInflater.from(context);
        this.dateFmt = DateFormat.getDateInstance(DateFormat.MEDIUM, Locale.getDefault());
        if (initial != null) items.addAll(initial);
        this.listener = listener;
        this.photoPlaceholderRes = photoPlaceholderRes;

    }

    public void setData(List<Individual> data) {
        items.clear();
        if (data != null) items.addAll(data);
        hhcount = 0;
        first = false;
        notifyDataSetChanged();

    }

    @Override public int getCount() { return items.size(); }
    @Override public Individual getItem(int position) { return items.get(position); }

    @Override public long getItemId(int position) {
        Individual ind = items.get(position);
        return (ind != null ) ? ind.uuid.hashCode() : position;
    }

    //private static int headCount = 0;

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {

        ViewHolder vh;
        if (convertView == null)
        {
            convertView = inflater.inflate(R.layout.item_individual, parent, false);
            vh = new ViewHolder(convertView);
            convertView.setTag(vh);
        }
        else
        {
            vh = (ViewHolder) convertView.getTag();
        }

        Individual item = getItem(position);

        if(item.relationship == 0 )
        {
            hhcount ++;
                    //= householdRegistrationService.hohCount(item.householdId);
        }

        vh.tvErrorMessage.setVisibility(View.GONE);

        if( hhcount > 1 && !first) {
            vh.tvErrorMessage.setVisibility(View.VISIBLE);
            vh.tvErrorMessage.setText("Please ensure only one person is designated as the head of household.");
            first = true;
        }
        else
        {

            CustomValidationResponse response = listener.isValid(item);

            if(!response.isValid)
            {

                vh.tvErrorMessage.setVisibility(View.VISIBLE);
                vh.tvErrorMessage.setText(response.message);
            }
            else
            {
                vh.tvErrorMessage.setVisibility(View.GONE);
            }


        }

        vh.tvIndividualId.setText("Individual # " + item.individualId);
        // Full name: first + middle + last (skip empties)
        String fullName = joinNonEmpty(" ",
                nz(item.firstName, null),
                nz(item.middleName, null),
                nz(item.lastName, null));
        vh.tvFullName.setText("Name: " + (isBlank(fullName) ? "—" : fullName));


        // Age
        String ageLabel = buildAgeLabel(item.dob, item.ageInYears, item.ageInMonths, item.ageInDays);
        vh.tvAge.setText("Age: " + ageLabel);

        // Gender / Relationship via lookup
        String gender = safeLookup(listener, item.gender, ReservedLookups.LKP_GENDERS);
        String relationship = safeLookup(listener, item.relationship, ReservedLookups.LKP_RELATIONSHIPS);
        vh.tvGender.setText("   •   Gender: " + nz(gender, "-"));
        vh.tvRelationship.setText("Rel: " + nz(relationship, "-"));


        // Photo (base64)
        vh.imgPhoto.setImageResource(photoPlaceholderRes);
        Bitmap bmp = ImageHelper.decodeBase64(item.photoBase64, ImageHelper.dpToPx(vh.imgPhoto, 72), ImageHelper.dpToPx(vh.imgPhoto, 72));
        if (bmp != null) {
            vh.imgPhoto.setImageBitmap(bmp);
        }

        // Biometric badge
        vh.tvBiometricBadge.setText("BIO");
        int badgeColor = item.isBiometricCollected() ? 0xFF2E7D32 /*green*/ : 0xFF9E9E9E /*gray*/;
        // Requires a shape background (bg_badge_round). Apply tint:
        if (vh.tvBiometricBadge.getBackground() != null) {
            vh.tvBiometricBadge.getBackground().setColorFilter(badgeColor, PorterDuff.Mode.SRC_IN);
        } else {
            vh.tvBiometricBadge.setBackgroundColor(badgeColor);
        }

        // Button click handlers — capture the current item/position
        vh.btnEdit.setOnClickListener(v -> {
            if (listener != null) listener.onEditClicked(item, position);
        });
        vh.btnDelete.setOnClickListener(v -> {
            if (listener != null) listener.onDeleteClicked(item, position);
        });


        return convertView;
    }

    private static String safeLookup(OnItemActionListener lp, int id, int domain) {
        try {
            return lp != null ? lp.getLookupItemLabel(id, domain) : null;
        } catch (Exception e) {
            return null;
        }
    }

    private static String joinNonEmpty(String sep, String... parts) {
        StringBuilder sb = new StringBuilder();
        for (String p : parts) {
            if (!isBlank(p)) {
                if (sb.length() > 0) sb.append(sep);
                sb.append(p);
            }
        }
        return sb.toString();
    }

    private static String nz(String s, String fallback) {
        return isBlank(s) ? (fallback == null ? "" : fallback) : s;
    }

    private static boolean isBlank(String s) {
        return s == null || s.trim().isEmpty();
    }

    /*private static int dpToPx(View v, int dp) {
        float d = v.getResources().getDisplayMetrics().density;
        return Math.max(1, (int) (dp * d + 0.5f));
    }*/


    private static String buildAgeLabel(Date dobOpt,
                                        Integer yearsOpt,
                                        Integer monthsOpt,
                                        Integer daysOpt) {
        // If dob present → compute years
        if (dobOpt != null ) {
            int years = yearsBetween(dobOpt, new Date());
            return String.valueOf(years);
        }
        // Else fall back to triplet (any that are present)
        boolean hasYears = yearsOpt != null;
        boolean hasMonths = monthsOpt != null;
        boolean hasDays = daysOpt != null ;

        if (!hasYears && !hasMonths && !hasDays) return "-";

        StringBuilder sb = new StringBuilder();
        if (hasYears) sb.append(yearsOpt).append("y");
        if (hasMonths) {
            if (sb.length() > 0) sb.append(" ");
            sb.append(monthsOpt).append("m");
        }
        if (hasDays) {
            if (sb.length() > 0) sb.append(" ");
            sb.append(daysOpt).append("d");
        }
        return sb.toString();
    }

    private static int yearsBetween(Date from, Date to) {
        Calendar c1 = Calendar.getInstance(Locale.getDefault());
        Calendar c2 = Calendar.getInstance(Locale.getDefault());
        c1.setTime(from);
        c2.setTime(to);
        int years = c2.get(Calendar.YEAR) - c1.get(Calendar.YEAR);
        // if birthday hasn’t occurred yet this year, subtract one
        if (c2.get(Calendar.DAY_OF_YEAR) < c1.get(Calendar.DAY_OF_YEAR)) {
            years--;
        }
        return Math.max(0, years);
    }

    private static class ViewHolder {
        final ImageView imgPhoto;
        final TextView tvIndividualId,tvErrorMessage, tvBiometricBadge, tvFullName;
        final TextView tvAge, tvGender, tvRelationship;

        final Button btnEdit, btnDelete;

        ViewHolder(View v) {
            imgPhoto = v.findViewById(R.id.imgPhoto);
            tvIndividualId = v.findViewById(R.id.tvIndividualId);
            tvErrorMessage = v.findViewById(R.id.tvErrorMessage);
            tvBiometricBadge = v.findViewById(R.id.tvBiometricBadge);
            tvFullName = v.findViewById(R.id.tvFullName);
            tvAge = v.findViewById(R.id.tvAge);
            tvGender = v.findViewById(R.id.tvGender);
            tvRelationship = v.findViewById(R.id.tvRelationship);
            btnEdit = v.findViewById(R.id.btnEdit);
            btnDelete = v.findViewById(R.id.btnDelete);

        }


    }
}
