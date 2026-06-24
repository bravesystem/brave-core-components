package intl.iom.bravemobile.helpers;

import android.os.Handler;
import android.text.Editable;
import android.text.TextWatcher;

import java.util.Map;

import intl.iom.bravemobile.models.datapoints.CollectionUnit;

public abstract class DebouncedTextWatcher implements TextWatcher {

    private final long delayMs;
    private final Handler handler = new Handler();
    private Runnable pending;

    protected DebouncedTextWatcher(long delayMs) {
        this.delayMs = delayMs;
    }

    public abstract void onDebouncedTextChanged(CharSequence s);

    @Override public void beforeTextChanged(CharSequence s, int start, int count, int after) { }

    @Override
    public void onTextChanged(CharSequence s, int start, int before, int count) {
        if (pending != null) handler.removeCallbacks(pending);
        final CharSequence text = s.toString();
        pending = () -> onDebouncedTextChanged(text);
        handler.postDelayed(pending, delayMs);
    }

    @Override public void afterTextChanged(Editable s) { }
}