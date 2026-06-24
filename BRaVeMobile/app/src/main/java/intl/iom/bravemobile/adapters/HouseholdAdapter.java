package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.PorterDuff;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.ImageView;
import android.view.ViewGroup;
import android.widget.BaseAdapter;
import android.widget.Button;
import android.widget.TextView;

import java.text.DateFormat;
import java.util.ArrayList;
import java.util.Calendar;
import java.util.Date;
import java.util.List;
import java.util.Locale;
import java.util.Optional;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.models.CustomValidationResponse;
import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.registrations.Household;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.statics.KnownLkp;
import intl.iom.bravemobile.statics.ReservedLookups;

public class HouseholdAdapter extends BaseAdapter {

    public interface OnItemActionListener {
        /** e.g., getLookupItemLabel(1, "gender") -> "Male" */
        //String getLookupItemLabel(int id, String domain);
        String getLookupItemLabel(int id, int domain);
        Individual getHeadOfHousehold(Household household);

        CustomValidationResponse isValid(Household household);

        void onEditClicked(Household item, int position);
        void onDeleteClicked(Household item, int position);
    }

    private final LayoutInflater inflater;
    private final DateFormat dateFmt;
    private final List<Household> items = new ArrayList<>();
    private final HouseholdAdapter.OnItemActionListener listener;

    private final int photoPlaceholderRes; // e.g., R.drawable.ic_no_photo

    public HouseholdAdapter(Context context,
                                       List<Household> initial,
                            HouseholdAdapter.OnItemActionListener listener,
                            int photoPlaceholderRes) {
        this.inflater = LayoutInflater.from(context);
        this.dateFmt = DateFormat.getDateInstance(DateFormat.MEDIUM, Locale.getDefault());
        if (initial != null) items.addAll(initial);
        this.listener = listener;
        this.photoPlaceholderRes = photoPlaceholderRes;
    }

    public void setData(List<Household> data) {
        items.clear();
        if (data != null) items.addAll(data);
        notifyDataSetChanged();
    }
    @Override public int getCount() { return items.size(); }
    @Override public Household getItem(int position) { return items.get(position); }

    @Override public long getItemId(int position) {
        Household hh = items.get(position);
        return (hh != null && hh.householdId != null) ? hh.householdId.hashCode() : position;
    }

    @Override
    public View getView(int position, View convertView, ViewGroup parent) {

        ViewHolder vh;
        if (convertView == null) {
            convertView = inflater.inflate(R.layout.item_household, parent, false);
            vh = new ViewHolder(convertView);
            convertView.setTag(vh);
        } else {
            vh = (ViewHolder) convertView.getTag();
        }

        Household item = getItem(position);

        vh.tvHouseholdId.setText("Household: " + nz(item.householdId, "—"));

        Individual head = listener.getHeadOfHousehold(item);

        if(item.fromServer)
        {
            vh.tvServerBadge.setVisibility(View.VISIBLE);
        }
        else
        {
            vh.tvServerBadge.setVisibility(View.GONE);
        }

        if(head==null)
        {
            vh.tvErrorMessage.setVisibility(View.VISIBLE);
            vh.tvErrorMessage.setText("Incomplete data.");
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

        // Full name: first + middle + last (skip empties)
        String fullName = head==null?"-":joinNonEmpty(" ",
                nz(head.firstName, null),
                nz(head.middleName, null),
                nz(head.lastName, null));
        vh.tvFullName.setText("Name: " + (isBlank(fullName) ? "—" : fullName));

        // Registration token (show only if present)
        if (!isBlank(item.registrationToken)) {
            vh.tvRegistrationToken.setVisibility(View.VISIBLE);
            vh.tvRegistrationToken.setText("Token: " + item.registrationToken);
        } else {
            vh.tvRegistrationToken.setVisibility(View.GONE);
        }

        // Age
        String ageLabel = head==null?"-":buildAgeLabel(head.dob, head.ageInYears, head.ageInMonths, head.ageInDays);
        vh.tvAge.setText("Age: " + ageLabel);

        // Gender / Relationship via lookup
        String gender = head==null?"-":safeLookup(listener, head.gender, ReservedLookups.LKP_GENDERS);
        String relationship = head==null?"-":safeLookup(listener, head.relationship, ReservedLookups.LKP_RELATIONSHIPS);
        vh.tvGender.setText("   •   Gender: " + nz(gender, "-"));
        vh.tvRelationship.setText("Rel: " + nz(relationship, "-"));

        // Household size
        vh.tvHouseholdSize.setText("Household size: " + item.getHouseholdSize());

        // Photo (base64)
        vh.imgPhoto.setImageResource(photoPlaceholderRes);
        Bitmap bmp = null;

        if(head!=null)
            bmp=ImageHelper.decodeBase64(head.photoBase64, dpToPx(vh.imgPhoto, 72), dpToPx(vh.imgPhoto, 72));

        if (bmp != null) {
            vh.imgPhoto.setImageBitmap(bmp);
        }

        // Biometric badge
        vh.tvBiometricBadge.setText("BIO");
        int badgeColor = head!=null && head.isBiometricCollected() ? 0xFF2E7D32 /*green*/ : 0xFF9E9E9E /*gray*/;
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

    private static int dpToPx(View v, int dp) {
        float d = v.getResources().getDisplayMetrics().density;
        return Math.max(1, (int) (dp * d + 0.5f));
    }


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
        final TextView tvHouseholdId,tvErrorMessage, tvBiometricBadge, tvFullName, tvRegistrationToken;
        final TextView tvAge, tvGender, tvRelationship, tvHouseholdSize, tvServerBadge;

        final Button btnEdit, btnDelete;

        ViewHolder(View v) {
            imgPhoto = v.findViewById(R.id.imgPhoto);
            tvHouseholdId = v.findViewById(R.id.tvHouseholdId);
            tvErrorMessage = v.findViewById(R.id.tvErrorMessage);
            tvBiometricBadge = v.findViewById(R.id.tvBiometricBadge);
            tvFullName = v.findViewById(R.id.tvFullName);
            tvRegistrationToken = v.findViewById(R.id.tvRegistrationToken);
            tvAge = v.findViewById(R.id.tvAge);
            tvGender = v.findViewById(R.id.tvGender);
            tvRelationship = v.findViewById(R.id.tvRelationship);
            tvHouseholdSize = v.findViewById(R.id.tvHouseholdSize);
            tvServerBadge = v.findViewById(R.id.tvServerBadge);
            btnEdit = v.findViewById(R.id.btnEdit);
            btnDelete = v.findViewById(R.id.btnDelete);

        }


    }
}
