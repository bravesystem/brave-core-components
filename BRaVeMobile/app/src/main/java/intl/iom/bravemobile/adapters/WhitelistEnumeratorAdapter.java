package intl.iom.bravemobile.adapters;

import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.Enumerator;

public class WhitelistEnumeratorAdapter extends RecyclerView.Adapter<WhitelistEnumeratorAdapter.VH>{

    private static class Row {
        final String code;
        final String nameOrNote;
        Row(String code, String nameOrNote) { this.code = code; this.nameOrNote = nameOrNote; }
    }

    private final List<Row> rows = new ArrayList<>();

    public WhitelistEnumeratorAdapter(List<String> whitelistCodes,
                                      List<Enumerator> allEnumerators) {
        buildRows(whitelistCodes, allEnumerators);
        setHasStableIds(true);
    }

    private void buildRows(List<String> whitelistCodes, List<Enumerator> all) {
        rows.clear();
        // index all enumerators by code
        Map<String, Enumerator> byCode = new HashMap<>();
        if (all != null) for (Enumerator e : all) if (e != null && e.code != null) byCode.put(e.code.toLowerCase(), e);
        // preserve whitelist order
        if (whitelistCodes != null) {
            for (String code : whitelistCodes) {
                Enumerator e = byCode.get(code.toLowerCase());
                if (e != null) rows.add(new Row(code, e.fullName));
                else rows.add(new Row(code, "(not found)"));
            }
        }
    }

    /** Call this to refresh data later (e.g., whitelist or enumerators changed). */
    public void setData(List<String> whitelistCodes, List<Enumerator> allEnumerators) {
        buildRows(whitelistCodes, allEnumerators);
        notifyDataSetChanged();
    }

    @Override public long getItemId(int position) {
        // stable-ish id from code
        return rows.get(position).code.hashCode();
    }

    @NonNull @Override
    public VH onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View v = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_enumerator_whitelist, parent, false);
        return new VH(v);
    }

    @Override
    public void onBindViewHolder(@NonNull VH h, int position) {
        Row r = rows.get(position);
        h.tvCode.setText(r.code);
        h.tvName.setText(r.nameOrNote);
    }

    @Override public int getItemCount() { return rows.size(); }

    static class VH extends RecyclerView.ViewHolder {
        final TextView tvCode, tvName;
        VH(@NonNull View itemView) {
            super(itemView);
            tvCode = itemView.findViewById(R.id.tvCode);
            tvName = itemView.findViewById(R.id.tvName);
        }
    }
}
