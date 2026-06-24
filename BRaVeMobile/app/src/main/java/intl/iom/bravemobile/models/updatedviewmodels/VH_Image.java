package intl.iom.bravemobile.models.updatedviewmodels;

import android.content.Context;
import android.graphics.Bitmap;
import android.text.InputType;
import android.view.LayoutInflater;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageView;
import android.widget.LinearLayout;
import android.widget.TextView;

import java.util.Map;

import intl.iom.bravemobile.R;
import intl.iom.bravemobile.helpers.DebouncedTextWatcher;
import intl.iom.bravemobile.helpers.ImageHelper;
import intl.iom.bravemobile.helpers.StringUtils;
import intl.iom.bravemobile.interfaces.ViewValidator;
import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.services.CameraCaptureHelper;

public class VH_Image extends LinearLayout implements ViewValidator {

    SurveyDataContext surveyDataContext;
    final TextView tvOrder, tvLabel;
    final LinearLayout llWrapper;
    final ImageView ivImage;
    final Button btnSelectImage, btnClearImage;
    final TextView tvRequired;
    final int questionId;

    private CameraCaptureHelper cameraHelper;

    public VH_Image(int questionId, Context context, SurveyDataContext surveyDataContext) {

        super(context);

        this.cameraHelper = surveyDataContext.cameraHelper;

        this.questionId = questionId;

        this.surveyDataContext = surveyDataContext;

        View v = LayoutInflater.from(context).inflate(R.layout.item_dp_image, this, true);

        llWrapper = v.findViewById(R.id.llWrapper);
        tvOrder = v.findViewById(R.id.tvOrder);
        tvLabel = v.findViewById(R.id.tvLabel);
        ivImage = v.findViewById(R.id.ivImage);
        btnSelectImage = v.findViewById(R.id.btnSelectImage);
        btnClearImage = v.findViewById(R.id.btnClearImage);
        tvRequired = v.findViewById(R.id.tvRequired);

        surveyDataContext.itemViews.put(questionId, this);

        bind();
    }

    private void bind( )
    {
        CollectionUnit dp = surveyDataContext.questions.get(questionId);
        Map<Integer, String> answers = surveyDataContext.answers;

        if(!answers.containsKey(dp.id))
            answers.put(dp.id, null);

        tvOrder.setText(String.format("Q.%d",dp.order));

        tvLabel.setText(dp.getDefaultText());

        String base64 = answers.get(dp.id);

        if(!StringUtils.isBlank(base64))
        {
            Bitmap bitmap = ImageHelper.decodeBase64(base64, ImageHelper.dpToPx(ivImage, 72), ImageHelper.dpToPx(ivImage, 72));
            ivImage.setImageBitmap(bitmap);
        }
        else
        {
            ivImage.setImageResource(android.R.drawable.ic_menu_report_image);

        }

        btnSelectImage.setOnClickListener(new OnClickListener() {
            @Override
            public void onClick(View view) {

                cameraHelper.capture(new CameraCaptureHelper.Listener() {
                    @Override public void onCaptured(CameraCaptureHelper.Result r) {

                        // preview
                        if (r.bitmap != null) ivImage.setImageBitmap(r.bitmap);

                        answers.put(questionId, r.base64);

                        // keep base64 / file path on your model
                        //photoBase64 = r.base64;
                        // r.filePath has the saved full image

                        //show or hide here
                        surveyDataContext.bubbleDown(dp.id, dp.order);
                    }
                    @Override public void onCanceled() {
                        // user canceled – optional toast
                    }
                    @Override public void onError(Exception e) {
                        // show error
                    }
                });

            }
        });

        btnClearImage.setOnClickListener(new OnClickListener() {
            @Override
            public void onClick(View view) {
                ivImage.setImageResource(android.R.drawable.ic_menu_report_image);
                answers.put(questionId, null);

                //show or hide here
                surveyDataContext.bubbleDown(dp.id, dp.order);
            }
        });

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

}
