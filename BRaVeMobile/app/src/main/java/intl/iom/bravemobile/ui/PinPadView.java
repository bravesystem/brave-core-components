package intl.iom.bravemobile.ui;

import android.content.Context;
import android.util.AttributeSet;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.TextView;

import androidx.annotation.Nullable;
import androidx.constraintlayout.widget.ConstraintLayout;

import com.google.android.material.button.MaterialButton;

import intl.iom.bravemobile.R;

public class PinPadView extends ConstraintLayout {

    public interface Listener {
        /** Called on every length change (0..max). */
        void onLengthChanged(int len);
        /** Called when PIN reached max length. */
        void onCompleted(String pin);
    }

    private static final int DEFAULT_MAX_LEN = 4;

    private TextView tvTitle, tvUser, tvError;
    private View dot1, dot2, dot3, dot4, llError;
    private MaterialButton btn0, btn1, btn2, btn3, btn4, btn5, btn6, btn7, btn8, btn9, btnClear, btnBackspace;

    private final StringBuilder pin = new StringBuilder(DEFAULT_MAX_LEN);
    private int maxLen = DEFAULT_MAX_LEN;
    private Listener listener;

    public PinPadView(Context context) { super(context); init(context); }
    public PinPadView(Context c, @Nullable AttributeSet a) { super(c, a); init(c); }
    public PinPadView(Context c, @Nullable AttributeSet a, int s) { super(c, a, s); init(c); }

    private void init(Context ctx) {
        LayoutInflater.from(ctx).inflate(R.layout.view_pin_pad, this, true);

        tvTitle = findViewById(R.id.tvPinTitle);
        tvUser  = findViewById(R.id.tvUser);
        tvError = findViewById(R.id.tvError);
        llError = findViewById(R.id.llError);

        dot1 = findViewById(R.id.dot1);
        dot2 = findViewById(R.id.dot2);
        dot3 = findViewById(R.id.dot3);
        dot4 = findViewById(R.id.dot4);

        btn0 = findViewById(R.id.btn0);  btn1 = findViewById(R.id.btn1);
        btn2 = findViewById(R.id.btn2);  btn3 = findViewById(R.id.btn3);
        btn4 = findViewById(R.id.btn4);  btn5 = findViewById(R.id.btn5);
        btn6 = findViewById(R.id.btn6);  btn7 = findViewById(R.id.btn7);
        btn8 = findViewById(R.id.btn8);  btn9 = findViewById(R.id.btn9);
        btnClear = findViewById(R.id.btnClear);
        btnBackspace = findViewById(R.id.btnBackspace);

        View.OnClickListener digitClick = v -> {
            if (pin.length() >= maxLen) return;
            char d = mapButtonToDigit(v.getId());
            if (d != 0) {
                pin.append(d);
                renderDots();
                notifyChanged();
                if (pin.length() == maxLen && listener != null) listener.onCompleted(pin.toString());
            }
        };
        btn0.setOnClickListener(digitClick); btn1.setOnClickListener(digitClick);
        btn2.setOnClickListener(digitClick); btn3.setOnClickListener(digitClick);
        btn4.setOnClickListener(digitClick); btn5.setOnClickListener(digitClick);
        btn6.setOnClickListener(digitClick); btn7.setOnClickListener(digitClick);
        btn8.setOnClickListener(digitClick); btn9.setOnClickListener(digitClick);

        btnBackspace.setOnClickListener(v -> {
            if (pin.length() > 0) {
                pin.deleteCharAt(pin.length() - 1);
                renderDots();
                notifyChanged();
            }
        });

        btnClear.setOnClickListener(v -> {
            pin.setLength(0);
            renderDots();
            notifyChanged();
        });

        renderDots();
    }

    private char mapButtonToDigit(int id) {
        if (id == R.id.btn0) return '0';
        if (id == R.id.btn1) return '1';
        if (id == R.id.btn2) return '2';
        if (id == R.id.btn3) return '3';
        if (id == R.id.btn4) return '4';
        if (id == R.id.btn5) return '5';
        if (id == R.id.btn6) return '6';
        if (id == R.id.btn7) return '7';
        if (id == R.id.btn8) return '8';
        if (id == R.id.btn9) return '9';
        return 0;
    }

    private void renderDots() {
        View[] dots = new View[]{dot1, dot2, dot3, dot4};
        int len = pin.length();
        for (int i = 0; i < dots.length; i++) {
            dots[i].setBackgroundResource(i < len ? R.drawable.bg_pin_dot_filled
                    : R.drawable.bg_pin_dot_empty);
        }
    }

    private void notifyChanged() { if (listener != null) listener.onLengthChanged(pin.length()); }

    // ---------- Public API ----------
    public void setListener(@Nullable Listener l) { this.listener = l; }
    public void setTitle(CharSequence title) { tvTitle.setText(title); }
    public void setUserHint(@Nullable CharSequence hint) { tvUser.setText(hint == null ? "" : hint); }
    public void setError(@Nullable CharSequence msg) {
        if (msg == null || msg.length() == 0) {
            llError.setVisibility(GONE);
        } else {
            tvError.setText(msg);
            llError.setVisibility(VISIBLE);
        }
    }
    public void clearError() { llError.setVisibility(GONE); }
    public void reset() { pin.setLength(0); renderDots(); notifyChanged(); }
    public void setMaxLen(int max) {
        this.maxLen = Math.max(1, Math.min(6, max)); // clamp 1..6 (adjust as you like)
        if (pin.length() > this.maxLen) {
            pin.setLength(this.maxLen);
        }
        renderDots(); notifyChanged();
    }
    public int getLength() { return pin.length(); }
    public String getPin() { return pin.toString(); }

}
