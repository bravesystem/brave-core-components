package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.graphics.drawable.Drawable;
import android.text.format.DateFormat;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.core.graphics.drawable.DrawableCompat;
import androidx.recyclerview.widget.RecyclerView;

import java.util.Date;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.ObjectSerializer;
import intl.iom.bravemobile.models.distributions.Family;
import intl.iom.bravemobile.models.registrations.Individual;
import intl.iom.bravemobile.models.registrations.Verification;

public class VerificationAdapter extends RecyclerView.Adapter<VerificationAdapter.VerificationVH> {


    public void executeOnLoad() {

        if(items==null || items.size()==0)
            return;

        Verification v = items.get(0);

        if (!v.is_processed && !(v.updated_on>v.inserted_on))
        {
            if (listener != null) listener.findMatch(v.uuid);
        }
    }

    public interface OnItemActionListener
    {
        void getMatch(String uuid);
        void findMatch(String uuid);

        void deleteVerification(String uuid);
        void getEnrollment(Verification verification);

    }


    private final Context context;
    private final List<Verification> items;

    private final VerificationAdapter.OnItemActionListener listener;

    private boolean auth = false; // used by distribution with biometric auth only

    public VerificationAdapter(Context context, List<Verification> items, VerificationAdapter.OnItemActionListener listener) {
       this(context, items, false, listener);
    }

    public VerificationAdapter(Context context, List<Verification> items, boolean auth, VerificationAdapter.OnItemActionListener listener) {
        this.context = context;
        this.items = items;
        this.auth = auth;
        this.listener = listener;

    }

    public void setData(List<Verification> data) {
        items.clear();
        if (data != null) items.addAll(data);
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public VerificationVH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(context).inflate(R.layout.item_verification, parent, false);
        return new VerificationVH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull VerificationVH holder, int position) {
        Verification v = items.get(position);

        holder.tvUuid.setText(v.uuid);

        // Gender display
        String genderText = v.gender == 1 ? "Male" : (v.gender == 2 ? "Female" : "Gender: Unspecified");

        // Convert inserted_on to readable date/time
        // If inserted_on is in seconds, multiply by 1000. If already ms, use as-is.
        long ts = v.updated_on;
        if (ts < 10_000_000_000L) { // heuristic: treat as seconds if < year 2286 in seconds
            ts = ts * 1000L;
        }
        String dateStr = DateFormat.format("MMM d, yyyy h:mm a", new Date(ts)).toString();

        holder.tvInfo.setText(genderText + " • " + dateStr);

        //String m

        // Optional data text
        if (v.data != null && !v.data.trim().isEmpty()) {
            holder.tvData.setVisibility(View.VISIBLE);
            holder.tvData.setText("Click the button to view the matching family\n");
        } /*else {
            holder.tvData.setVisibility(View.GONE);
        }*/

        // Badge logic:
        // - NOT is_processed -> "Just saved" (orange)
        // - is_processed && matched_found -> "Match found" (red) + show button
        // - is_processed && NOT matched_found -> "No match" (green)
        String badgeText;
        int badgeColor;

        if(auth){

            Family f = ObjectSerializer.deserialize(v.data, Family.class);

            if(v.matched_found && !f.isEnrolled())
            {
                holder.btnDelivery.setVisibility(View.VISIBLE);

                holder.btnDelivery.setOnClickListener(view -> {

                    if (listener != null) listener.getEnrollment(v);

                });
            }
            else if(v.matched_found && f.isEnrolled())
            {
                holder.btnDelivery.setVisibility(View.GONE);
                holder.tvData.setText("Click the button to view the matching family\nAn enrollment already exists for this family. Re-enrollment is not permitted.");
            }

            holder.btnClear.setVisibility(View.VISIBLE);

            holder.btnClear.setOnClickListener(view -> {

                if (listener != null) listener.deleteVerification(v.uuid);

            });

        }

        if (!v.is_processed)
        {
            badgeText = v.updated_on>v.inserted_on
                    ?"Processing"
                    :"Saved";

            holder.tvData.setText("Click the button to find a matching family");
            badgeColor = context.getResources().getColor(R.color.neutral_grey);
            holder.btnViewMatch.setVisibility(View.GONE);
            holder.btnFindMatchInd.setVisibility(View.VISIBLE);

            holder.btnFindMatchInd.setOnClickListener(view -> {

                if (listener != null) listener.findMatch(v.uuid);

            });

        }
        else if (v.matched_found) {
            badgeText = "Match found";
            badgeColor = context.getResources().getColor(R.color.badge_match_found_bg);
            holder.btnViewMatch.setVisibility(View.VISIBLE);

            holder.btnFindMatchInd.setVisibility(View.GONE);

            holder.btnViewMatch.setOnClickListener(view -> {

                if (listener != null) listener.getMatch(v.uuid);

                /*String msg = "Matched family for UUID: " + v.uuid +
                        (v.matched_uuid != null ? (" (match: " + v.matched_uuid + ")") : "");
                Toast.makeText(context, msg, Toast.LENGTH_SHORT).show();*/

            });


        } else {
            badgeText = "No match";
            badgeColor = context.getResources().getColor(R.color.badge_no_match_bg);
            holder.btnViewMatch.setVisibility(View.GONE);
            holder.btnFindMatchInd.setVisibility(View.GONE);

            holder.tvData.setVisibility(View.GONE);
        }

        holder.tvBadge.setText(badgeText);

        // Tint rounded badge background
        Drawable bg = holder.tvBadge.getBackground();
        Drawable wrapped = DrawableCompat.wrap(bg).mutate();
        DrawableCompat.setTint(wrapped, badgeColor);
        holder.tvBadge.setBackground(wrapped);
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class VerificationVH extends RecyclerView.ViewHolder {
        TextView tvUuid, tvBadge, tvInfo, tvData;
        Button btnViewMatch, btnFindMatchInd;
        Button btnDelivery, btnClear;

        VerificationVH(@NonNull View itemView) {
            super(itemView);
            tvUuid = itemView.findViewById(R.id.tvUuid);
            tvBadge = itemView.findViewById(R.id.tvBadge);
            tvInfo = itemView.findViewById(R.id.tvInfo);
            tvData = itemView.findViewById(R.id.tvData);
            btnViewMatch = itemView.findViewById(R.id.btnViewMatch);
            btnFindMatchInd = itemView.findViewById(R.id.btnSyncIndVerif);
            //
            btnDelivery = itemView.findViewById(R.id.btnDelivery);
            btnClear = itemView.findViewById(R.id.btnClear);
        }
    }
}
