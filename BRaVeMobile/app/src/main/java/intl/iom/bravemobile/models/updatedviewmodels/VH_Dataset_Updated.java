package intl.iom.bravemobile.models.updatedviewmodels;

import android.app.DatePickerDialog;
import android.content.Context;
import android.text.InputType;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.Spinner;
import android.widget.Switch;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.Nullable;
import androidx.recyclerview.widget.RecyclerView;

import com.google.gson.Gson;
import com.google.gson.JsonArray;
import com.google.gson.JsonElement;
import com.google.gson.JsonNull;
import com.google.gson.JsonObject;
import com.google.gson.JsonParser;

import java.util.ArrayList;
import java.util.Calendar;
import java.util.Collections;
import java.util.List;
import java.util.Locale;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.DatasetColumnsProvider;
import intl.iom.bravemobile.interfaces.OptionsProvider;
import intl.iom.bravemobile.interfaces.PreValidateAction;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.viewmodels.VH_Dataset;

public class VH_Dataset_Updated extends LinearLayout implements ViewValidator, PreValidateAction {
    final SurveyDataContext surveyDataContext;
    final TextView tvOrder,tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final TableLayout parentContainer;
    final Button btnAddEntry;
    private List<DatasetColumn> columns;
    private  LayoutInflater inflater;
    private Context context;

    private final List<TableRow> entryRows = new ArrayList<>();

    private OptionsProvider optionsProvider;

    final  int questionId;

    public VH_Dataset_Updated(int questionId, Context context, SurveyDataContext surveyDataContext ) {

        super(context);

        this.context = context;

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dataset, this, true);

        this.context = v.getContext();

        inflater = LayoutInflater.from( context );
        llWrapper = v.findViewById(R.id.item_dataset);
        tvLabel = v.findViewById(R.id.tvLabel);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvRequired = v.findViewById(R.id.tvRequired);
        btnAddEntry = v.findViewById(R.id.btnAddEntry);
        parentContainer = v.findViewById(R.id.parentContainer);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }
    public void bind()
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;
        optionsProvider = surveyDataContext.optionsProvider;
        DatasetColumnsProvider datasetColumnsProvider = surveyDataContext.datasetColumnsProvider;

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        columns = datasetColumnsProvider.getDatasetColumnsFor(dp.datasetId.get());

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());

        // 4) Restore selection from saved answer (Integer value id)
        String existing = answers.get(dp.id);

        loadRowsFromJsonGson(existing);

        btnAddEntry.setOnClickListener(v -> addEntryRow());

    }

    public void loadRowsFromJsonGson(String json) {

        if(json==null)
            return;
        // Clear existing
        parentContainer.removeAllViews();
        entryRows.clear();

        JsonElement root = JsonParser.parseString(json);
        if (!root.isJsonArray()) {
            Toast.makeText(context, "Invalid JSON.", Toast.LENGTH_SHORT).show();
            return;
        }
        JsonArray arr = root.getAsJsonArray();

        for (JsonElement el : arr) {
            if (!el.isJsonObject()) continue;
            JsonObject record = el.getAsJsonObject();

            TableRow row = addEntryRow();

            for (int c = 0; c < columns.size(); c++) {
                DatasetColumn col = columns.get(c);
                String key = String.valueOf(col.id);

                Object value = null;
                if (record.has(key) && !record.get(key).isJsonNull()) {
                    JsonElement v = record.get(key);
                    switch (col.type) {
                        case 1: // int
                            value = v.getAsInt();
                            break;
                        case 2: // numeric
                            value = v.getAsDouble();
                            break;
                        case 3: // text
                        case 4: // date (string)
                            value = v.getAsString();
                            break;
                        case 5: // boolean
                            value = v.getAsBoolean();
                            break;
                        case 6: // dropdown (stored value)
                            // Could be String or Number; preserve as string for matching
                            value = v.isJsonPrimitive() ? v.getAsJsonPrimitive().getAsString() : v.toString();
                            break;
                        default:
                            value = v.toString();
                    }
                }

                View cell = row.getChildAt(c);
                setValueIntoCell(cell, col, value);
            }

            //parentContainer.addView(row);
            //entryRows.add(row);
        }

        //Toast.makeText(this, "Loaded " + arr.size() + " record(s).", Toast.LENGTH_SHORT).show();
    }

    private void onSubmitAll() {
        for (TableRow row : entryRows) {
            if (!validateRow(row)) {
                //Toast.makeText(this, "Please complete required fields.", Toast.LENGTH_SHORT).show();
                surveyDataContext.answers.put(questionId,  null);
                return;
            }

        }


        JsonArray records = new JsonArray();

        for (TableRow row : entryRows) {
            JsonObject record = new JsonObject();

            for (int i = 0; i < row.getChildCount(); i++) {
                View cell = row.getChildAt(i);
                if (cell instanceof Button) continue;

                View input = findInputInCell(cell);
                if (input == null) continue;

                VH_Dataset_Updated.FieldMeta meta = (VH_Dataset_Updated.FieldMeta) input.getTag();
                if (meta == null) continue;

                String key = String.valueOf(meta.columnId);

                switch (meta.type) {
                    case 1: { // integer
                        String s = ((EditText) input).getText().toString().trim();
                        if (!s.isEmpty()) record.addProperty(key, Integer.parseInt(s)); else record.add(key, JsonNull.INSTANCE);
                        break;
                    }
                    case 2: { // numeric
                        String s = ((EditText) input).getText().toString().trim();
                        if (!s.isEmpty()) record.addProperty(key, Double.parseDouble(s)); else record.add(key, JsonNull.INSTANCE);
                        break;
                    }
                    case 3: { // text
                        record.addProperty(key, ((EditText) input).getText().toString());
                        break;
                    }
                    case 4: { // date
                        record.addProperty(key, ((EditText) input).getText().toString());
                        break;
                    }
                    case 5: { // boolean
                        record.addProperty(key, ((Switch) input).isChecked());
                        break;
                    }
                    case 6: { // dropdown
                        Object obj = ((Spinner) input).getSelectedItem();
                        if (obj instanceof SelectItem) {
                            record.addProperty(key, ((SelectItem)obj).getValue());
                        } else if (obj != null) {
                            record.addProperty(key, obj.toString());
                        } else {
                            record.add(key, JsonNull.INSTANCE);
                        }
                        break;
                    }
                }
            }

            records.add(record);
        }

        String jsonString
                = new Gson().toJson(records);

        surveyDataContext.answers.put(questionId, jsonString);

        //show or hide here
        surveyDataContext.bubbleDown(questionId, surveyDataContext.questions.get(questionId).order);

        Log.d("DATASET_JSON", jsonString);
        //Toast.makeText(this, "Saved " + records.size() + " record(s).", Toast.LENGTH_SHORT).show();
    }

    private TableRow addEntryRow() {
        TableRow row = new TableRow(context);

        // Let the row grow as wide as needed (so HS can scroll)
        TableLayout.LayoutParams rowLp = new TableLayout.LayoutParams(
                TableLayout.LayoutParams.WRAP_CONTENT,
                TableLayout.LayoutParams.WRAP_CONTENT
        );
        rowLp.setMargins(dp(0), dp(8), dp(0), dp(8));
        row.setLayoutParams(rowLp);
        row.setPadding(dp(0), dp(4), dp(0), dp(4));

        // Build each field cell horizontally
        for (int i = 0; i < columns.size(); i++) {
            DatasetColumn col = columns.get(i);
            View fieldView = createFieldView(col);
            if (fieldView != null) {

                // WRAP_CONTENT so it keeps natural width and can overflow
                TableRow.LayoutParams cellLp = new TableRow.LayoutParams(
                        TableRow.LayoutParams.WRAP_CONTENT,
                        TableRow.LayoutParams.WRAP_CONTENT
                );

                // Add horizontal spacing between cells
                // (Right margin except for the last cell)
                if (i < columns.size() - 1) {
                    cellLp.setMarginEnd(dp(12));
                    // For older APIs, also set right margin:
                    cellLp.rightMargin = dp(12);
                }

                fieldView.setLayoutParams(cellLp);

                // (Optional) ensure inner inputs aren’t tiny
                applyMinWidthIfNeeded(fieldView, col);

                row.addView(fieldView);
            }
        }


// --- Add the Remove button as the LAST element ---
        Button btnRemove = new Button(context);
        btnRemove.setText("✕"); // or "Remove"
        btnRemove.setAllCaps(false);
        btnRemove.setContentDescription("Remove this row");

        // Make it compact
        TableRow.LayoutParams rmLp = new TableRow.LayoutParams(
                TableRow.LayoutParams.WRAP_CONTENT,
                TableRow.LayoutParams.WRAP_CONTENT
        );
        // Add some left margin so it doesn't stick to the last field
        rmLp.setMarginStart(dp(8));
        rmLp.leftMargin = dp(8);
        btnRemove.setLayoutParams(rmLp);

        // (Optional) make it visually lighter/smaller
        btnRemove.setMinWidth(dp(36));
        btnRemove.setPadding(dp(12), dp(6), dp(12), dp(6));

        // Click → remove this row
        btnRemove.setOnClickListener(v -> {
            parentContainer.removeView(row);
            entryRows.remove(row);
        });

        row.addView(btnRemove);
        // --- end remove button ---


        parentContainer.addView(row);
        entryRows.add(row);

        return row;
    }

    private void applyMinWidthIfNeeded(View fieldView, DatasetColumn col) {
        // You can tune these per type
        final int minText = dp(160);
        final int minDate = dp(140);
        final int minSpin = dp(140);

        switch (col.type) {
            case 1: // int
            case 2: // numeric
            case 3: // text
            {
                EditText et = fieldView.findViewById(R.id.etValue);
                if (et != null) et.setMinWidth(minText);
                break;
            }
            case 4: // date
            {
                EditText etDate = fieldView.findViewById(R.id.etDate);
                if (etDate != null) etDate.setMinWidth(minDate);
                break;
            }
            case 6: // dropdown
            {
                Spinner sp = fieldView.findViewById(R.id.spValue);
                if (sp != null) sp.setMinimumWidth(minSpin);
                break;
            }
        }
    }

    private int dp(int v) {
        return Math.round(context.getResources().getDisplayMetrics().density * v);
    }

    private String buildTitle(String base, boolean required) {
        return required ? base + " *" : base;
    }
    private View createFieldView(DatasetColumn col) {
        LayoutInflater inflater = LayoutInflater.from(context);
        View view;

        switch (col.type) {
            case 1: // int
            case 2: // numeric
            case 3: // text
                view = inflater.inflate(R.layout.field_text, null, false);
                setupTextField(view, col);
                break;

            case 4: // date
                view = inflater.inflate(R.layout.field_date, null, false);
                setupDateField(view, col);
                break;

            case 5: // boolean
                view = inflater.inflate(R.layout.field_switch, null, false);
                setupSwitchField(view, col);
                break;

            case 6: // dropdown
                view = inflater.inflate(R.layout.field_spinner, null, false);
                setupSpinnerField(view, col);
                break;

            default:
                return null;
        }

        return view;
    }

    private void setValueIntoCell(View cell, DatasetColumn col, Object value) {
        if (cell == null || col == null) return;

        switch (col.type) {
            case 1: // int
            case 2: // numeric
            case 3: { // text
                EditText et = cell.findViewById(R.id.etValue);
                if (et != null) et.setText(value != null ? String.valueOf(value) : "");
                break;
            }
            case 4: { // date (expects string like "yyyy-MM-dd")
                EditText etDate = cell.findViewById(R.id.etDate);
                if (etDate != null) etDate.setText(value != null ? String.valueOf(value) : "");
                break;
            }
            case 5: { // boolean (Switch)
                Switch sw = cell.findViewById(R.id.swValue);
                if (sw != null) sw.setChecked(value instanceof Boolean ? (Boolean) value
                        : (value != null && "true".equalsIgnoreCase(String.valueOf(value))));
                break;
            }
            case 6: { // dropdown (SelectItem {value,label}) — we stored value
                Spinner sp = cell.findViewById(R.id.spValue);
                if (sp != null) {
                    Object targetValue = value; // may be String/Integer depending on SelectItem.value type
                    ArrayAdapter<?> adapter = (ArrayAdapter<?>) sp.getAdapter();
                    if (adapter != null) {
                        for (int i = 0; i < adapter.getCount(); i++) {
                            Object obj = adapter.getItem(i);
                            if (obj instanceof SelectItem) {
                                SelectItem si = (SelectItem) obj;
                                // Compare as strings to be robust
                                if ( targetValue != null
                                        && Integer.toString(si.getValue()).equals(targetValue.toString())) {
                                    sp.setSelection(i);
                                    break;
                                }
                            }
                        }
                    }
                }
                break;
            }
        }
    }


    private View findInputInCell(View cellRoot) {
        if (cellRoot instanceof LinearLayout) {
            LinearLayout ll = (LinearLayout) cellRoot;
            for (int i = 0; i < ll.getChildCount(); i++) {
                View v = ll.getChildAt(i);
                if (v instanceof EditText || v instanceof Spinner || v instanceof Switch) {
                    return v;
                }
            }
        }
        return null;
    }


    private boolean validateRow(TableRow row) {
        for (int i = 0; i < row.getChildCount(); i++) {
            View cell = row.getChildAt(i);

            // Find the input view (EditText, Switch, Spinner) in the cell
            View input = findInputInCell(cell);
            if (input == null) continue;

            VH_Dataset_Updated.FieldMeta meta = (VH_Dataset_Updated.FieldMeta) input.getTag();
            if (meta == null) continue;

            if (!meta.required) continue;

            boolean ok = false;

            switch (meta.type) {
                case 1:
                case 2:
                case 3:
                case 4:  // text-like
                    EditText et = (EditText) input;
                    String val = et.getText().toString().trim();
                    ok =  !StringUtils.isBlank(val);
                    if(!ok)
                        return false;
                    break;

                /*case 5 -> { // boolean
                    // boolean required usually doesn't make sense; treat as must be true?
                    Switch sw = (Switch) input;
                    return !meta.required || !sw.isChecked();
                }*/
                case 6 :  // spinner
                    Spinner sp = (Spinner) input;
                    //yield sp.getSelectedItemPosition() >= 0 && sp.getAdapter() != null && sp.getAdapter().getCount() > 0;
                    ok =  sp.getSelectedItemPosition()>0;
                    if(!ok)
                        return false;
                    break;
                default :
                    break;
            };



        }
        return true;
    }

    private void setupTextField(View root, DatasetColumn col) {
        TextView tvTitle = root.findViewById(R.id.tvTitle);
        EditText etValue = root.findViewById(R.id.etValue);

        tvTitle.setText(buildTitle(col.title, col.required));

        // Input types based on col.type
        if (col.type == 1) {
            etValue.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_SIGNED);
        } else if (col.type == 2) {
            etValue.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_DECIMAL | InputType.TYPE_NUMBER_FLAG_SIGNED);
        } else {
            etValue.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES);
        }

        etValue.setTag(new VH_Dataset_Updated.FieldMeta(col.id, col.type, col.required, col.lookupId));
    }

    private void setupDateField(View root, DatasetColumn col) {
        TextView tvTitle = root.findViewById(R.id.tvTitle);
        EditText etDate = root.findViewById(R.id.etDate);

        tvTitle.setText(buildTitle(col.title, col.required));
        etDate.setHint("Select date");
        etDate.setTag(new VH_Dataset_Updated.FieldMeta(col.id, col.type, col.required, col.lookupId));

        etDate.setOnClickListener(v -> {
            final Calendar c = Calendar.getInstance();
            int y = c.get(Calendar.YEAR), m = c.get(Calendar.MONTH), d = c.get(Calendar.DAY_OF_MONTH);
            DatePickerDialog dp = new DatePickerDialog(
                    context,
                    (view, year, monthOfYear, dayOfMonth) -> {
                        String mm = String.format(Locale.US, "%02d", monthOfYear + 1);
                        String dd = String.format(Locale.US, "%02d", dayOfMonth);
                        etDate.setText(year + "-" + mm + "-" + dd); // ISO-like yyyy-MM-dd
                    },
                    y, m, d
            );
            dp.show();
        });
    }

    private void setupSwitchField(View root, DatasetColumn col) {
        TextView tvTitle = root.findViewById(R.id.tvTitle);
        Switch swValue = root.findViewById(R.id.swValue);

        tvTitle.setText(buildTitle(col.title, col.required));
        swValue.setTag(new VH_Dataset_Updated.FieldMeta(col.id, col.type, col.required, col.lookupId));
    }

    private void setupSpinnerField(View root, DatasetColumn col) {
        TextView tvTitle = root.findViewById(R.id.tvTitle);
        Spinner spValue = root.findViewById(R.id.spValue);

        tvTitle.setText(buildTitle(col.title, col.required));

        List<SelectItem> options = (col.lookupId != null) ? optionsProvider.getOptionsFor(col.lookupId) : Collections.emptyList();

        // Optional: add a "Select..." placeholder if the field is required
        if (col.required) {
            List<SelectItem> withPlaceholder = new ArrayList<>();
            withPlaceholder.add(new SelectItem(-1, "Select..."));
            withPlaceholder.addAll(options);
            options = withPlaceholder;
        }

        ArrayAdapter<SelectItem> adapter =
                new ArrayAdapter<>(context, android.R.layout.simple_spinner_item, options);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        spValue.setAdapter(adapter);


        spValue.setTag(new VH_Dataset_Updated.FieldMeta(col.id, col.type, col.required, col.lookupId));
    }

    @Override
    public void setValidation(String message) {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        tvRequired.setVisibility( dp.isRequired ? View.VISIBLE : View.GONE);
        tvRequired.setText(message);
    }

    @Override
    public void clearValidation() {
        tvRequired.setVisibility(View.GONE);
        tvRequired.setText(null);
    }

    @Override
    public void submitAll() {

        Map<Integer, String> answers = surveyDataContext.answers;

        for (TableRow row : entryRows) {
            if (!validateRow(row)) {
                //Toast.makeText(this, "Please complete required fields.", Toast.LENGTH_SHORT).show();
                answers.put(questionId,  null);
                return;
            }
        }

        JsonArray records = new JsonArray();

        for (TableRow row : entryRows) {
            JsonObject record = new JsonObject();

            for (int i = 0; i < row.getChildCount(); i++) {
                View cell = row.getChildAt(i);
                if (cell instanceof Button) continue;

                View input = findInputInCell(cell);
                if (input == null) continue;

                VH_Dataset_Updated.FieldMeta meta = (VH_Dataset_Updated.FieldMeta) input.getTag();
                if (meta == null) continue;

                String key = String.valueOf(meta.columnId);

                switch (meta.type) {
                    case 1: { // integer
                        String s = ((EditText) input).getText().toString().trim();
                        if (!s.isEmpty()) record.addProperty(key, Integer.parseInt(s)); else record.add(key, JsonNull.INSTANCE);
                        break;
                    }
                    case 2: { // numeric
                        String s = ((EditText) input).getText().toString().trim();
                        if (!s.isEmpty()) record.addProperty(key, Double.parseDouble(s)); else record.add(key, JsonNull.INSTANCE);
                        break;
                    }
                    case 3: { // text
                        record.addProperty(key, ((EditText) input).getText().toString());
                        break;
                    }
                    case 4: { // date
                        record.addProperty(key, ((EditText) input).getText().toString());
                        break;
                    }
                    case 5: { // boolean
                        record.addProperty(key, ((Switch) input).isChecked());
                        break;
                    }
                    case 6: { // dropdown
                        Object obj = ((Spinner) input).getSelectedItem();
                        if (obj instanceof SelectItem) {
                            record.addProperty(key, ((SelectItem)obj).getValue());
                        } else if (obj != null) {
                            record.addProperty(key, obj.toString());
                        } else {
                            record.add(key, JsonNull.INSTANCE);
                        }
                        break;
                    }
                }
            }

            records.add(record);
        }

        String jsonString
                = new Gson().toJson(records);

        answers.put(questionId, jsonString);

        Log.d("DATASET_JSON", jsonString);

    }

    @Override
    public boolean validate() {
        return false;
    }


    static class FieldMeta {
        int columnId;
        int type;
        boolean required;
        @Nullable
        Integer lookupId;
        FieldMeta(int columnId, int type, boolean required, @Nullable Integer lookupId) {
            this.columnId = columnId;
            this.type = type;
            this.required = required;
            this.lookupId = lookupId;
        }
    }


}
