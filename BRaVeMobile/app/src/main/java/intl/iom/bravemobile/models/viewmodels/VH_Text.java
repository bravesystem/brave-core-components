package intl.iom.bravemobile.models.viewmodels;

import android.text.InputType;
import android.view.View;
import android.widget.EditText;
import android.widget.LinearLayout;
import android.widget.TextView;

import androidx.constraintlayout.widget.ConstraintLayout;
import androidx.constraintlayout.widget.ConstraintSet;
import androidx.recyclerview.widget.RecyclerView;

import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.adapters.CollectionUnitAdapter;
import intl.iom.bravemobile.helpers.DebouncedTextWatcher;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public class VH_Text extends RecyclerView.ViewHolder implements CollectionUnitAdapter.ConstraintValidator{
    final TextView tvOrder, tvLabel;
    final LinearLayout llWrapper;
    final EditText etValue;
    final TextView tvRequired;

    public VH_Text(View v) {
        super(v);
        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        etValue = v.findViewById(R.id.etValue);
        tvRequired = v.findViewById(R.id.tvRequired);
    }


    public void bind(Map<Integer, CollectionUnitAdapter.ConstraintValidator> viewHolders, Map<Integer, Boolean> vhVisibilityById, Map<Integer, CollectionUnit> questionTypes, Map<Integer, String> answers, CollectionUnitAdapter.AnswerListener listener, CollectionUnit dp, boolean showOrder)
    {
        viewHolders.put(dp.id, this);

        /*if (vhVisibilityById.get(dp.id))
        {
            show();
        }
        else
        {
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
        etValue.setInputType(InputType.TYPE_CLASS_TEXT | InputType.TYPE_TEXT_FLAG_CAP_SENTENCES);
        String existing = answers.get(dp.id);
        etValue.setText(existing instanceof String ? (String) existing : "");

        /*etValue.setOnFocusChangeListener((view, hasFocus) -> {
            if (!hasFocus) {
                String val = etValue.getText().toString();
                answers.put(dp.id, val);
                if (listener != null) listener.onAnswerChanged(dp, val.toString());
            }
        });*/

        etValue.addTextChangedListener(new DebouncedTextWatcher(600) {
            @Override
            public void onDebouncedTextChanged(CharSequence s) {
                String text = (s == null) ? "" : s.toString().trim();

                // No value -> clear answer
                if (text.isEmpty()) {
                    answers.put(dp.id, null);
                    return;
                }

                // Valid input: store raw string
                answers.put(dp.id, text);
            }
        });

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

        // 2. Clear the constraints tied to llWrapper
        /*ConstraintLayout parent = (ConstraintLayout) this.itemView;

        ConstraintSet set = new ConstraintSet();
        set.clone(parent);

        set.clear(llWrapper.getId());  // <— collapse it fully

        set.applyTo(parent);*/

    }

    @Override
    public void show() {
        llWrapper.setVisibility(View.VISIBLE);

        // Restore constraints (optional but good practice)
        /*ConstraintSet set = new ConstraintSet();
        set.clone((ConstraintLayout) this.itemView);
        set.applyTo((ConstraintLayout) this.itemView);*/
    }
}
