package intl.iom.bravemobile.adapters;

import android.graphics.Bitmap;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.distributions.Family;

public class VerifMemberAdapter extends RecyclerView.Adapter<VerifMemberAdapter.VerifMemberVH> {

    private final List<Family.Member> members = new ArrayList<>();
    private String highlightUuid; // uuid to highlight

    public VerifMemberAdapter(List<Family.Member> initialMembers, String highlightUuid) {
        if (initialMembers != null) this.members.addAll(initialMembers);
        this.highlightUuid = highlightUuid;
    }

    public void setHighlightUuid(String uuid) {
        this.highlightUuid = uuid;
        notifyDataSetChanged();
    }

    public void setMembers(List<Family.Member> newMembers) {
        members.clear();
        if (newMembers != null) members.addAll(newMembers);
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public VerifMemberVH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_verif_member, parent, false);
        return new VerifMemberVH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull VerifMemberVH h, int position) {
        Family.Member m = members.get(position);

        // Text
        h.tvFullName.setText(safe(m.getFullName()));
        h.tvRelationship.setText(safe(m.getRelationship()));
        h.tvGenderAge.setText(safe(m.getGender()) + " • " + m.getAge());

        // Photo

        h.ivPhoto.setImageResource(R.drawable.ic_no_photo_24);
        //Bitmap photo = decodeBase64ToBitmap(m.getPhotoB64());
        if(!StringUtils.isBlank(m.getPhotoB64())){

            Bitmap photo = ImageHelper.decodeBase64(m.getPhotoB64(), ImageHelper.dpToPx(h.ivPhoto, 72), ImageHelper.dpToPx(h.ivPhoto, 72));
            if (photo != null) {
                h.ivPhoto.setImageBitmap(photo);
            } /*else {
                h.ivPhoto.setImageResource(R.drawable.ic_no_photo_24); // create/replace
            }*/

        }


        // BIO badge state
        boolean hasBio = m.isBiometricCollected();

        h.tvBioBadge.setBackgroundResource(
                hasBio ? R.drawable.bg_bio_badge_green : R.drawable.bg_bio_badge_gray
        );

        // Highlight matching uuid
        boolean isHighlight = !isNullOrEmpty(highlightUuid)
                && highlightUuid.equalsIgnoreCase(safe(m.getUuid()));

        h.root.setBackgroundResource(
                isHighlight ? R.drawable.bg_member_row_highlight : R.drawable.bg_member_row_normal
        );
    }

    @Override
    public int getItemCount() {
        return members.size();
    }

    static class VerifMemberVH extends RecyclerView.ViewHolder {
        View root;
        ImageView ivPhoto;
        TextView tvFullName, tvRelationship, tvGenderAge, tvBioBadge;

        VerifMemberVH(@NonNull View itemView) {
            super(itemView);
            root = itemView.findViewById(R.id.root);
            ivPhoto = itemView.findViewById(R.id.ivPhoto);
            tvFullName = itemView.findViewById(R.id.tvFullName);
            tvRelationship = itemView.findViewById(R.id.tvRelationship);
            tvGenderAge = itemView.findViewById(R.id.tvGenderAge);
            tvBioBadge = itemView.findViewById(R.id.tvBioBadge);
        }
    }

    private static String safe(String s) {
        return s == null ? "" : s;
    }

    private static boolean isNullOrEmpty(String s) {
        return s == null || s.trim().isEmpty();
    }

    /*private static Bitmap decodeBase64ToBitmap(String b64) {
        if (isNullOrEmpty(b64)) return null;

        try {
            // If your base64 includes "data:image/png;base64," prefix, strip it
            int comma = b64.indexOf(',');
            String clean = (comma >= 0) ? b64.substring(comma + 1) : b64;

            byte[] data = Base64.decode(clean, Base64.DEFAULT);
            return BitmapFactory.decodeByteArray(data, 0, data.length);
        } catch (Exception ex) {
            return null;
        }
    }*/
}
