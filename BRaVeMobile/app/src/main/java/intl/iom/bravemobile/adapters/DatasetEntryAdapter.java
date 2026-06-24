package intl.iom.bravemobile.adapters;

import android.view.LayoutInflater;
import android.view.ViewGroup;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.viewmodels.VH_Boolean;
import intl.iom.bravemobile.models.viewmodels.VH_Date;
import intl.iom.bravemobile.models.viewmodels.VH_MA_SelectOne;
import intl.iom.bravemobile.models.viewmodels.VH_Number;
import intl.iom.bravemobile.models.viewmodels.VH_Text;
import intl.iom.bravemobile.statics.AnswerType;

public class DatasetEntryAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder>{


    private final List<DatasetColumn> columns;
    // Holds the entered values for the CURRENT record being edited.
    // key = column.id, value = user input (String, Integer, BigDecimal/Double, Boolean, LocalDate/String, Integer for select)
    private final Map<Integer, Object> values = new HashMap<>();
    private LayoutInflater inflater;

    public DatasetEntryAdapter(List<DatasetColumn> columns) {
        this.columns = columns != null ? columns : new ArrayList<>();
        setHasStableIds(true);
    }

    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        // Uses the provided function to get the correct layout holder for the type.
        inflater = LayoutInflater.from(parent.getContext());
        return getLayout(parent, viewType);
    }

    private RecyclerView.ViewHolder getLayout(ViewGroup parent, int viewType) {
        AnswerType t = AnswerType.fromCode(viewType);
        switch (t) {
            case INT:
            case NUMERIC:
                return new VH_Number(inflater.inflate(R.layout.item_dp_number, parent, false));
            case DATE:
                return new VH_Date(inflater.inflate(R.layout.item_dp_date, parent, false));
            case BOOLEAN:
                return new VH_Boolean(inflater.inflate(R.layout.item_dp_boolean, parent, false));
            case SELECT_ONE:
                //return new VH_SelectOne(inflater.inflate(R.layout.item_dp_select_one_searchable, parent, false));
                return new VH_MA_SelectOne(inflater.inflate(R.layout.item_dp_select_one_material_auto_complete, parent, false));
            case TEXT:
            default:
                return new VH_Text(inflater.inflate(R.layout.item_dp_text, parent, false));
        }
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {

        DatasetColumn col = columns.get(position);
        AnswerType t = AnswerType.fromCode(col.type);
        switch (t) {
            default:
                // Fallback: treat as text if unknown
                bindText(holder, col);
        }

    }

    private void bindText(RecyclerView.ViewHolder holder, DatasetColumn col) {
    }

    @Override
    public long getItemId(int position) {
        return columns.get(position).id;
    }


    @Override
    public int getItemViewType(int position) {
        // Directly use the type enum provided
        return columns.get(position).type;
    }

    @Override
    public int getItemCount() {
        return columns.size();
    }

}
