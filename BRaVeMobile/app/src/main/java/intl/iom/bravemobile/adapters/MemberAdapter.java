package intl.iom.bravemobile.adapters;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.helpers.StringUtils;
import  intl.iom.bravemobile.models.distributions.Family;

import androidx.recyclerview.widget.RecyclerView;

import android.graphics.Bitmap;
import android.graphics.PorterDuff;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.ImageView;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.HashSet;
import java.util.List;
import java.util.Set;

public class MemberAdapter extends RecyclerView.Adapter<MemberAdapter.VH> {

    public interface SelectionCallback {
        void onSelectionChanged(Set<String> selectedUuids);
    }

    private final List<Family.Member> members;
    private final boolean singleSelect;
    private final SelectionCallback callback;

    private String selectedUuidSingle = null;
    private final Set<String> selectedUuidsMulti = new HashSet<>();

    private final int photoPlaceholderRes;

    public MemberAdapter(List<Family.Member> members, boolean singleSelect, SelectionCallback callback, int photoPlaceholderRes) {
        this.members = members;
        this.singleSelect = singleSelect;
        this.callback = callback;
        this.photoPlaceholderRes = photoPlaceholderRes;
    }

    static class VH extends RecyclerView.ViewHolder {
        CheckBox cbSelect;
        TextView tvFullName;
        TextView tvRelationship;
        TextView tvGenderAge;
        TextView tvBioBadge;
        ImageView ivPhoto;

        VH(@NonNull View itemView) {
            super(itemView);
            cbSelect = itemView.findViewById(R.id.cbSelect);
            tvFullName = itemView.findViewById(R.id.tvFullName);
            tvRelationship = itemView.findViewById(R.id.tvRelationship);
            tvGenderAge = itemView.findViewById(R.id.tvGenderAge);
            tvBioBadge = itemView.findViewById(R.id.tvBioBadge);
            ivPhoto = itemView.findViewById(R.id.ivPhoto);
        }
    }

    @NonNull
    @Override
    public VH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_member, parent, false);
        return new VH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull VH holder, int position) {
        Family.Member m = members.get(position);

        holder.tvFullName.setText(m.getFullName());
        String rel = m.getRelationship();
        if (rel != null && rel.length() > 0) {
            rel = Character.toUpperCase(rel.charAt(0)) + rel.substring(1);
        }
        holder.tvRelationship.setText(rel);
        holder.tvGenderAge.setText(m.getGender() + " • " + m.getAge());

        holder.cbSelect.setOnCheckedChangeListener(null);

        holder.ivPhoto.setImageResource(photoPlaceholderRes);

        if(!StringUtils.isBlank(m.getPhotoB64()))
        {
            Bitmap bmp = ImageHelper.decodeBase64(m.getPhotoB64(), ImageHelper.dpToPx(holder.ivPhoto, 72), ImageHelper.dpToPx(holder.ivPhoto, 72));
            if (bmp != null) {
                holder.ivPhoto.setImageBitmap(bmp);
            }
        }

        int badgeColor = m.isBiometricCollected() ? 0xFF2E7D32 /*green*/ : 0xFF9E9E9E /*gray*/;

        holder.tvBioBadge.setBackgroundColor(badgeColor);

        boolean checked;
        if (singleSelect) {
            checked = m.getUuid().equals(selectedUuidSingle);
        } else {
            checked = selectedUuidsMulti.contains(m.getUuid());
        }
        holder.cbSelect.setChecked(checked);

        holder.cbSelect.setOnCheckedChangeListener((buttonView, isChecked) -> {
            if (singleSelect) {
                String prev = selectedUuidSingle;
                if (isChecked) {
                    selectedUuidSingle = m.getUuid();
                    if (prev != null && !prev.equals(m.getUuid())) {
                        int prevIndex = indexOfUuid(prev);
                        if (prevIndex >= 0) notifyItemChanged(prevIndex);
                    }
                } else {
                    if (m.getUuid().equals(selectedUuidSingle)) {
                        selectedUuidSingle = null;
                    }
                }
                if (callback != null) callback.onSelectionChanged(getCurrentSelection());
            } else {
                if (isChecked) {
                    selectedUuidsMulti.add(m.getUuid());
                } else {
                    selectedUuidsMulti.remove(m.getUuid());
                }
                if (callback != null) callback.onSelectionChanged(getCurrentSelection());
            }
        });

        // Toggle checkbox when the row is clicked
        holder.itemView.setOnClickListener(v -> holder.cbSelect.setChecked(!holder.cbSelect.isChecked()));
    }

    @Override
    public int getItemCount() {
        return members.size();
    }

    private int indexOfUuid(String uuid) {
        for (int i = 0; i < members.size(); i++) {
            if (uuid.equals(members.get(i).getUuid())) return i;
        }
        return -1;
    }

    public Set<String> getCurrentSelection() {
        if (singleSelect) {
            Set<String> s = new HashSet<>();
            if (selectedUuidSingle != null) s.add(selectedUuidSingle);
            return s;
        } else {
            return new HashSet<>(selectedUuidsMulti);
        }
    }
}

