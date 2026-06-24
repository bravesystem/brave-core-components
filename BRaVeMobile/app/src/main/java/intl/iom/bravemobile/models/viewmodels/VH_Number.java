package intl.iom.bravemobile.models.viewmodels;


import android.text.InputFilter;
import android.text.InputType;
import android.view.View;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.List;
import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.DebouncedTextWatcher;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.rules.RuleEngineRevised;
import intl.iom.bravemobile.rules.SkipLogicResult;
import intl.iom.bravemobile.statics.AnswerType;

// NUMBER (INT/NUMERIC)
public class VH_Number extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator {

    private final String TAG = VH_Number.class.getSimpleName();
    final TextView tvOrder,tvLabel, tvRequired;
    final LinearLayout llWrapper;
    final EditText etValue;

    public VH_Number(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        etValue = v.findViewById(R.id.etValue);
        tvRequired = v.findViewById(R.id.tvRequired);
    }

    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer,CollectionUnit> questionList, Map<Integer, String> answers, CollectionUnitAdapter.AnswerListener listener, CollectionUnit dp, boolean showOrder)
    {
        viewHolders.put(dp.id, this);

        /*if (vhVisibilityById.get(dp.id))
        {
            show();
        } else {
            hide();
        }*/

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        isRequired = dp.isRequired;

        if(showOrder)
            tvOrder.setText(String.format("Q.%d",dp.order));
        else
            tvOrder.setVisibility(View.GONE);

        tvLabel.setText(dp.getDefaultText());
        //tvRequired.setVisibility(dp.isRequired ? View.VISIBLE : View.GONE);

        etValue.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_DECIMAL | InputType.TYPE_NUMBER_FLAG_SIGNED);

        // If strictly INT, you can restrict decimals:
        if (dp.answerType == AnswerType.INT) {
            etValue.setInputType(InputType.TYPE_CLASS_NUMBER | InputType.TYPE_NUMBER_FLAG_SIGNED);
            // optional: limit length
            etValue.setFilters(new InputFilter[]{ new InputFilter.LengthFilter(10) });
        }

        Object existing = answers.get(dp.id);
        etValue.setText(existing != null ? String.valueOf(existing) : "");

        etValue.addTextChangedListener(new DebouncedTextWatcher(600) {
            @Override
            public void onDebouncedTextChanged(CharSequence s) {
                String text = (s == null) ? "" : s.toString().trim();

                // No value -> clear answer
                if (text.isEmpty()) {
                    answers.put(dp.id, null);
                    return;
                }

                if(text.equals(answers.get(dp.id)))
                    return;

                try {
                    // Validate numeric input if needed
                    if (dp.answerType == AnswerType.INT)
                    {
                        Integer.parseInt(text);
                    }
                    else if (dp.answerType == AnswerType.NUMERIC)
                    {
                        Double.parseDouble(text);
                    }

                    // Valid input: store raw string
                    answers.put(dp.id, text);

                    //evaluate skip logic
                    //SkipLogicResult result = RuleEngineRevised.evaluate(dp, questionTypes, answers);

                    //collapse views here

                } catch (NumberFormatException e) {
                    // Invalid -> clear answer
                    answers.put(dp.id, null);
                }

                /*if( !StringUtils.isBlank(dp.skipLogic) && RuleEngineRevised.evaluate(dp, questionList, answers).conditionTrue)
                {
                    vhVisibilityById.put(dp.id, true);
                }

                List<Map.Entry<Integer, CollectionUnit>> list =
                        new ArrayList<>(questionList.entrySet());

                Collections.sort(list, new Comparator<Map.Entry<Integer, CollectionUnit>>() {
                    @Override
                    public int compare(Map.Entry<Integer, CollectionUnit> e1,
                                       Map.Entry<Integer, CollectionUnit> e2) {

                        return Integer.compare(e1.getValue().order, e2.getValue().order);
                    }
                });

                boolean found = false;


                // Iterate in sorted order
                for (Map.Entry<Integer, CollectionUnit> entry : list) {
                    Integer id = entry.getKey();


                    if (!found) {
                        if (id == dp.id) {
                            found = true;
                            // If you want to INCLUDE the first x, call func here too:
                            // func(val);
                        }
                        continue;
                    }


                    CollectionUnit qv = entry.getValue();

                    SkipLogicResult result = RuleEngineRevised.evaluate(qv, questionList, answers);

                    //evaluate skip logic
                    if(result.conditionTrue)
                    {
                        vhVisibilityById.put(id, true);
                        viewHolders.get(id).show();
                    }
                    else if(result.parentIds.size()>0)
                    {
                        //answers.put(id, null);
                        vhVisibilityById.put(id, false);
                        viewHolders.get(id).hide();
                    }

                    //System.out.println(id + " => order=" + qv.order + ", visible=" + qv.visible);
                }

                 */

                //collapse views here


            }
        });

        /*etValue.setOnFocusChangeListener((view, hasFocus) -> {
            if (!hasFocus) {
                String s = etValue.getText().toString().trim();
                Object parsed = null;
                try {
                    parsed = (dp.answerType == AnswerType.INT) ? Integer.parseInt(s) :
                            (s.isEmpty() ? null : Double.parseDouble(s));
                } catch (Exception ignore) {}
                if (parsed == null && s.isEmpty()) {
                    answers.remove(dp.id);
                } else {
                    answers.put(dp.id, parsed.toString());
                }
                if (listener != null) listener.onAnswerChanged(dp, parsed.toString());
            }
        });*/
    }

    boolean isRequired;

    @Override
    public void onVisibilityChanged(int id, boolean visible) {

    }

    private boolean override = true;

    @Override
    public void setValidation(String message) {
        tvRequired.setVisibility(isRequired ? View.VISIBLE : View.GONE);
        if (override) {
            tvRequired.setText(message);
        }
        // else ignore and keep existing message
    }

    @Override
    public void setValidation(String message, boolean override) {
        this.override = override;
        tvRequired.setVisibility(isRequired ? View.VISIBLE : View.GONE);

        if (!override) {
            // allowed to update the message
            tvRequired.setText(message);
        }
        // else: just lock, do not override
    }


    @Override
    public void clearValidation() {
        override = true;
        tvRequired.setVisibility(View.GONE);
        tvRequired.setText(null);
    }


    @Override
    public void hide() {
        llWrapper.setVisibility(View.GONE);
    }

    @Override
    public void show() {
        llWrapper.setVisibility(View.VISIBLE);
    }

}
