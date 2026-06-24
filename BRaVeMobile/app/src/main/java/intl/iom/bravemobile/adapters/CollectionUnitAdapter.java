package intl.iom.bravemobile.adapters;

import intl.iom.bravemobile.helpers.DataCollectionUnitValidator;
import intl.iom.bravemobile.models.DatasetColumn;
import intl.iom.bravemobile.models.registrations.AdminLevel;
import intl.iom.bravemobile.rules.RuleEngineRevised;
import intl.iom.bravemobile.rules.SkipLogicResult;
import intl.iom.bravemobile.rules.SkipLogicVisibility;
import intl.iom.bravemobile.statics.AnswerType;

import android.content.Context;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.view.inputmethod.InputMethodManager;

import java.util.ArrayList;
import java.util.Collections;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.viewmodels.*;

public class CollectionUnitAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> implements SkipLogicVisibility {

    private final String TAG = CollectionUnitAdapter.class.getSimpleName();


    public interface DatasetColumnsProvider {
        /** Return the list of option labels for SELECT_ONE / SELECT_MULTIPLE. */
        List<DatasetColumn> getDatasetColumnsFor(int datasetId);
    }

    public interface OptionsProvider {
        /** Return the list of option labels for SELECT_ONE / SELECT_MULTIPLE. */
        List<SelectItem> getOptionsFor(String lookupName);
        List<SelectItem> getOptionsFor(int lookupId);

    }


    public interface SkipLogicListener {
        void onVisibilityChanged(int id, boolean visible);
    }


    public interface AdminLocationProvider
    {
        List<AdminLevel> getAdminLevelsUpTo(int Level);
        List<SelectItem> getOptionsFor(Integer parent);
    }


    public interface AnswerListener {
        /** Called whenever an answer changes; value type depends on answerType. */
        void onAnswerChanged(CollectionUnit dp, String value);
    }

    public interface DatasetListener {
        public void submitAll();

    }

    public interface ConstraintValidator {

        void onVisibilityChanged(int id, boolean visible);
        void setValidation(String message);
        void setValidation(String message, boolean override);
        void clearValidation();
        void hide();
        void show();

    }

    private final LayoutInflater inflater;
    private final OptionsProvider optionsProvider;
    private final AdminLocationProvider adminLocationProvider;
    private final DatasetColumnsProvider datasetColumnsProvider;
    private final AnswerListener listener;

    private final List<CollectionUnit> items = new ArrayList<>();
    private final Map<Integer, String> answers = new HashMap<>(); // dp.id -> value
    //private Map<Integer, AnswerType> questionTypes = new HashMap<>(); // dp.id -> value
    private Map<Integer, Boolean> vhVisibilityMById = new HashMap<>(); // dp.id -> value
    private final Map<Integer, ConstraintValidator> viewHolders = new HashMap<>(); // dp.id -> value
    private final boolean showOrder ;


    //new for admin level type
    public CollectionUnitAdapter(LayoutInflater inflater,DatasetColumnsProvider datasetColumnsProvider, OptionsProvider optionsProvider, AdminLocationProvider adminLocationProvider,AnswerListener listener, boolean showOrder)
    {
        this.inflater = inflater;
        this.datasetColumnsProvider = datasetColumnsProvider;
        this.optionsProvider = optionsProvider;
        this.adminLocationProvider = adminLocationProvider;
        this.listener = listener;
        this.showOrder = showOrder;
        setHasStableIds(true);
    }

    public CollectionUnitAdapter(LayoutInflater inflater,DatasetColumnsProvider datasetColumnsProvider, OptionsProvider optionsProvider, AnswerListener listener, boolean showOrder)
    {
        this(inflater,datasetColumnsProvider,optionsProvider,null,listener, showOrder);
    }


    public CollectionUnitAdapter(LayoutInflater inflater, DatasetColumnsProvider datasetColumnsProvider, OptionsProvider optionsProvider, AnswerListener listener, Map<Integer,String> answers, boolean showOrder) {
        this(inflater,datasetColumnsProvider,optionsProvider,listener, showOrder);
        if(answers!=null && answers.size()>0)
            this.answers.putAll(answers);
    }

    //new for admin level type
    public CollectionUnitAdapter(LayoutInflater inflater, DatasetColumnsProvider datasetColumnsProvider, OptionsProvider optionsProvider,AdminLocationProvider adminLocationProvider, AnswerListener listener, Map<Integer,String> answers, boolean showOrder) {
        this(inflater,datasetColumnsProvider,optionsProvider,adminLocationProvider,listener, showOrder);
        if(answers!=null && answers.size()>0)
            this.answers.putAll(answers);
    }


    public void clearAnyFocus(@NonNull RecyclerView rv) {
        View focusedChild = rv.getFocusedChild();        // the child view holding current focus
        if (focusedChild != null) {
            View focusedInChild = focusedChild.findFocus(); // e.g., an EditText inside
            if (focusedInChild != null) focusedInChild.clearFocus();
            focusedChild.clearFocus();
            InputMethodManager imm = (InputMethodManager)
                    rv.getContext().getSystemService(Context.INPUT_METHOD_SERVICE);
            if (imm != null) imm.hideSoftInputFromWindow(focusedChild.getWindowToken(), 0);
        }
    }

    private Map<Integer, CollectionUnit> questionList = new HashMap<>();

    public void submit(List<CollectionUnit> data) {
        items.clear();
        questionList.clear();

        if (data != null) {
            // optional: sort by order
            ArrayList<CollectionUnit> sorted = new ArrayList<>(data);
            Collections.sort(sorted, (a, b) -> Integer.compare(a.order, b.order));
            items.addAll(sorted);
        }

        //questionTypes = new HashMap<>();
        questionList = new HashMap<>();

        for(CollectionUnit c : items)
        {
            //questionTypes.put(c.id, c.answerType);
            questionList.put(c.id, c);
        }

        for(CollectionUnit c : items)
        {
            viewHolders.put(c.id, null);
        }

        for(CollectionUnit c : items)
        {
            vhVisibilityMById.put(c.id, false);
        }

        apply();

        notifyDataSetChanged();
    }


    @Override
    public void apply() {

        if (items == null) return;

        for(CollectionUnit c : items)
        {
            if (c == null) continue;

            // Case 1: no skip logic -> always show
            if (c.skipLogic == null || c.skipLogic.trim().isEmpty())
            {
                //show(raw);
                vhVisibilityMById.put(c.id,  true);
                continue;
            }

            // Case 2: evaluate skip-logic
            SkipLogicResult result = RuleEngineRevised.evaluate(c, questionList, answers);

            if (result == null || !result.conditionTrue && result.parentIds.size()>0)
            {
                // Condition is false or evaluation failed -> hide
                vhVisibilityMById.put(c.id,  false);
                continue;
            }

            // Case 3: condition is true; verify all parents precede this element

            boolean allParentsBefore = true;

            for (Integer parentId : result.parentIds) {
                if (parentId >= c.id) {
                    allParentsBefore = false;
                    break;
                }
            }

            if (allParentsBefore)
            {
                vhVisibilityMById.put(c.id, true);
            } else
            {
                vhVisibilityMById.put(c.id,  false);
            }

        }

    }


    /** Pre-fill an answer for a datapoint id. */
    public void setAnswer(int dataPointId, String value) {
        answers.put(dataPointId, value);
    }

    /** Get current answers map */
    public Map<Integer, String> getAnswers() {

        return answers;

    }

    @Override
    public long getItemId(int position)
    {
        return items.get(position).id;
    }

    @Override
    public int getItemViewType(int position) {
        AnswerType t = items.get(position).answerType;
        return t.getCode();
    }

    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(ViewGroup parent, int viewType) {

        try {
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
                case SELECT_MULTIPLE:
                    return new VH_SelectMultiple(inflater.inflate(R.layout.item_dp_select_multiple, parent, false));
                case DATASET:
                    return new VH_Dataset(inflater.inflate(R.layout.item_dataset, parent, false));
                case ADMINLEVEL:
                    return new VH_AdminLocation(inflater.inflate(R.layout.item_dp_admin_location, parent, false));
                case TEXT:
                default:
                    return new VH_Text(inflater.inflate(R.layout.item_dp_text, parent, false));
            }
        }catch (Exception e)
        {
            throw e;
        }
    }

    public boolean validate()
    {

        for(CollectionUnit item : items)
        {
            if(!validateCollectionUnit(item))
                return false;
        }
        return true;
    }

    public boolean validateCollectionUnit(CollectionUnit dp)
    {
            boolean is_required = dp.isRequired;

            if(!answers.containsKey(dp.id))
            {
                //show required text in view
                viewHolders.get(dp.id).setValidation("*");

                return false;
            }

            /*if(StringUtils.isBlank( answers.get(dp.id) ) && is_required)
            {
                //show required text in view
                viewHolders.get(dp.id).setValidation("*");

                return false;
            }*/

            if(dp.answerType == AnswerType.ADMINLEVEL)
            {
                ((VH_AdminLocation)viewHolders.get(dp.id)).submitAll();

                if(! ((VH_AdminLocation)viewHolders.get(dp.id)).validate() )
                    return false;
            }

            if(dp.answerType == AnswerType.DATASET)
            {
                ((VH_Dataset)viewHolders.get(dp.id)).submitAll();
            }

            if (!DataCollectionUnitValidator.validate(dp, viewHolders, questionList, answers, is_required))
            {
                //show required text in view
                if(viewHolders.get(dp.id)!=null)
                    viewHolders.get(dp.id).setValidation("Required");

                return false;
            }

            try
            {
                //equal null when full page not scrolled.
                if(viewHolders.get(dp.id)!=null)
                    viewHolders.get(dp.id).clearValidation();
            }
            catch (Exception e)
            {
                Log.e(TAG, e.getMessage());
            }

            return true;
    }

    @Override
    public void onBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        CollectionUnit dp = items.get(position);

        /*boolean visible = vhVisibilityMById.get(dp.id);

        if (!visible) {
            holder.itemView.setVisibility(View.GONE);
            return;  // IMPORTANT: do NOT bind hidden items
        } else {
            holder.itemView.setVisibility(View.VISIBLE);
        }*/


        try {
            switch (dp.answerType) {
                case INT:
                case NUMERIC:
                    ((VH_Number) holder).bind(viewHolders,vhVisibilityMById,questionList,answers, listener, dp, showOrder);
                    break;
                case DATE:
                    ((VH_Date) holder).bind(viewHolders,vhVisibilityMById,questionList,answers, listener, dp, showOrder);
                    break;
                case BOOLEAN:
                    ((VH_Boolean) holder).bind(viewHolders,vhVisibilityMById,questionList,answers, listener, dp, showOrder);
                    break;
                case SELECT_ONE:
                    ((VH_MA_SelectOne) holder).bind(viewHolders,vhVisibilityMById,questionList,answers, optionsProvider, listener, dp, showOrder);
                    break;
                case SELECT_MULTIPLE:
                    ((VH_SelectMultiple) holder).bind(viewHolders,vhVisibilityMById,questionList,answers, optionsProvider, listener, dp, showOrder);
                    break;
                case DATASET:
                    ((VH_Dataset) holder).bind(viewHolders,vhVisibilityMById,questionList,answers, datasetColumnsProvider,optionsProvider, listener, dp, showOrder);
                    break;
                case ADMINLEVEL:
                    ((VH_AdminLocation)holder).bind(viewHolders,vhVisibilityMById, questionList,answers, adminLocationProvider, dp, showOrder);
                    break;
                case TEXT:
                default:
                    ((VH_Text) holder).bind(viewHolders,vhVisibilityMById,questionList, answers, listener, dp, showOrder);
                    break;
            }
        }catch (Exception e)
        {
            throw e;
        }
    }

    @Override
    public int getItemCount() {
        return items.size();
        //Integer.max(items.size() , answers.size() );
    }
}
