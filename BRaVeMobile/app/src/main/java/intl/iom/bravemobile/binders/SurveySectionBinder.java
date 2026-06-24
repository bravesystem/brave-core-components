package intl.iom.bravemobile.binders;

import android.app.Activity;
import android.content.Context;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import intl.iom.bravemobile.R;

import intl.iom.bravemobile.models.SurveyDataContext;
import intl.iom.bravemobile.models.datapoints.CollectionUnit;
import intl.iom.bravemobile.models.updatedviewmodels.VH_AdminLocation_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_Boolean_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_Dataset_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_Date_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_Image;
import intl.iom.bravemobile.models.updatedviewmodels.VH_MA_SelectOne_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_Number_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_SelectMultiple_Updated;
import intl.iom.bravemobile.models.updatedviewmodels.VH_Text_Updated;
import intl.iom.bravemobile.models.viewmodels.VH_AdminLocation;
import intl.iom.bravemobile.statics.AnswerType;

public final class SurveySectionBinder {

    private SurveySectionBinder() {
        // no-op
    }

    /**
     * Bind a single question into the container in FillSurveyPageUpdated.
     * For now this only chooses the correct layout to inflate by type and
     * adds it to the container; value/label binding can be added later.
     */
    public static View bindSection(CollectionUnit question, Activity activity, SurveyDataContext surveyDataContext) throws Exception {

        if (question == null || activity == null) {
            throw new Exception();
        }

        ViewGroup container = activity.findViewById(R.id.containerParentComponents);

        if (container == null) {
            throw new Exception();
        }

        View itemView= getLayoutForType( question, activity, surveyDataContext);

        container.addView(itemView);

        return itemView;
    }

    /**
     * Decide which layout to use for a given AnswerType.
     * Any unsupported type falls back to the text item layout.
     */
    private static View getLayoutForType(CollectionUnit question, Context context, SurveyDataContext surveyDataContext) throws Exception {

        AnswerType answerType = question.answerType;

        if (answerType == null) {
           throw new Exception();
        }

         switch (answerType) {
            case TEXT:
            case NOTE:
            case COMPUTED:
                return new VH_Text_Updated(question.id, context, surveyDataContext);
             case PHOTO:
                 return new VH_Image(question.id, context, surveyDataContext);

            case DATE:
                return new VH_Date_Updated(question.id, context, surveyDataContext);

            case INT:
            case NUMERIC:
                return new VH_Number_Updated(question.id, context, surveyDataContext);

            case BOOLEAN:
                return new VH_Boolean_Updated(question.id, context, surveyDataContext);

            case SELECT_ONE:
                // Using the searchable select-one layout for all choice-like types for now
                return new VH_MA_SelectOne_Updated(question.id, context, surveyDataContext);
            case SELECT_MULTIPLE:
                // Using the searchable select-one layout for all choice-like types for now
                return new VH_SelectMultiple_Updated(question.id, context, surveyDataContext);
            case DATASET:
                // Using the searchable select-one layout for all choice-like types for now
                return new VH_Dataset_Updated(question.id, context, surveyDataContext);
            case ADMINLEVEL:
                // Using the searchable select-one layout for all choice-like types for now
                return new VH_AdminLocation_Updated(question.id, context, surveyDataContext);
            default:
                // Fallback: render as simple text question
                return new VH_Text_Updated(question.id, context, surveyDataContext);
        }
    }
}

