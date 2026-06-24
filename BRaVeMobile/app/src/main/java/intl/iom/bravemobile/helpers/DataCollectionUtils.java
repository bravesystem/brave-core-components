package intl.iom.bravemobile.helpers;

import android.os.Build;
import android.text.TextUtils;
import android.widget.EditText;

import java.time.LocalDate;
import java.time.Period;
import java.time.ZoneId;
import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.HashSet;
import java.util.List;
import java.util.Locale;
import java.util.Set;

import intl.iom.bravemobile.exceptions.DataPointError;
import intl.iom.bravemobile.exceptions.IndividualError;
import intl.iom.bravemobile.statics.AnswerType;
import intl.iom.bravemobile.models.datapoints.DataPoint;
import intl.iom.bravemobile.models.registrations.Individual;

public final class DataCollectionUtils {

    public static String fullname(String firstname, String middlename, String lastname) {
        StringBuilder sb = new StringBuilder();

        if (firstname != null && !firstname.trim().isEmpty()) {
            sb.append(firstname.trim());
        }
        if (middlename != null && !middlename.trim().isEmpty()) {
            if (sb.length() > 0) sb.append(" ");
            sb.append(middlename.trim());
        }
        if (lastname != null && !lastname.trim().isEmpty()) {
            if (sb.length() > 0) sb.append(" ");
            sb.append(lastname.trim());
        }

        return sb.toString();
    }


    public static boolean validHouseholdId(String deviceSignature, String input) {
        if (deviceSignature == null || input == null) return false;

        // Extract the first two digits from deviceSignature
        String prefix = deviceSignature.substring(0,2);

        String trimmed = input.trim();

        int len = trimmed.length();

        if (len < 10 || len > 11) return false;

        // Case-insensitive startsWith (robust even if prefix ever includes letters)
        return trimmed.regionMatches(true, 0, prefix, 0, prefix.length());
    }


    public static String householdId(String deviceSignature, int nextHouseholdId)
    {
        if (deviceSignature == null) {
            throw new IllegalArgumentException("deviceSignature must not be null");
        }
        if (nextHouseholdId < 0 || nextHouseholdId > 99_999) {
            throw new IllegalArgumentException("nextHouseholdId must be in [0, 99_999]");
        }
        return deviceSignature + String.format(Locale.US, "%05d", nextHouseholdId);
    }

    public static String getHouseholdId(String value) {

        if (value == null) return null;

        int idx = value.indexOf('_');

        // If no underscore, or underscore at start or end → return full string
        if (idx <= 0 || idx == value.length() - 1) {
            return value;
        }

        // Otherwise, return substring before underscore
        return value.substring(0, idx);
    }

    public static String getIndividualId(String householdId, int individualNo)
    {
        String value = String.valueOf(100+individualNo);

        String lastTwo = null;

        if(value!=null && value.length()>=2)
        {
            lastTwo = value.substring(value.length() -2 );
        }

        return String.format("%s_%s", householdId, lastTwo);
    }


    public static List<String> filterByPrefixFast(List<String> subjects, List<String> householdIds) {
        if (subjects == null || householdIds == null) return Collections.emptyList();

        final Set<String> prefixes = new HashSet<>(householdIds);
        final List<String> result = new ArrayList<>();

        for (String s : subjects) {
            if (s == null) continue;
            int i = s.indexOf('_');
            String head = (i >= 0) ? s.substring(0, i) : s;
            if (prefixes.contains(head)) {
                result.add(s);
            }
        }
        return result;
    }

    public static boolean dataPointIsValid(DataPoint data) throws DataPointError
    {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N)
        {
            if(data.answerType== AnswerType.SELECT_ONE && !data.lookupId.isPresent())
                throw new DataPointError("Lookup is required for Select One type.");

            if(data.answerType==AnswerType.SELECT_MULTIPLE && !data.lookupId.isPresent())
                throw new DataPointError("Lookup is required for Select Multiple type.");

            if(data.answerType==AnswerType.DATASET && !data.datasetId.isPresent())
                throw new DataPointError("Dataset is required for Dataset type.");
        }

        return true;
    }

    public static boolean individualIsValid(Individual data) throws IndividualError
    {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {

            if(data.dob==null && data.ageInDays==null
            &&  data.ageInMonths==null && data.ageInDays==null)
            {
                throw new IndividualError("Age is required");
            }
        }

        return true;
    }


    /**
     * Returns age in whole years based on the given fields, using the device's timezone.
     * Exclusivity rule: Either dob is set OR any of ageInYears/ageInMonths/ageInDays is set, but not both.
     *
     * @param dob            java.util.Date of birth (timestamp). If provided, we compute precise age based on today's date in the device timezone.
     * @param ageInYears     Used if dob is null. If present, returned as-is.
     * @param ageInMonths    Used if dob and ageInYears are null. Floors months/12 to whole years.
     * @param ageInDays      Used if dob, ageInYears, and ageInMonths are null. Floors days/365 to whole years.
     * @return               Age in whole years, or null if not computable.
     * @throws IllegalArgumentException if exclusivity is violated, dob is in the future, or negative values are provided.
     */
    public static Integer computeAgeInYearsUsingDeviceZone(Date dob,
                                                           Integer ageInYears,
                                                           Integer ageInMonths,
                                                           Integer ageInDays) {

        // Device (system) timezone
        final ZoneId deviceZone = ZoneId.systemDefault();
        final LocalDate today = LocalDate.now(deviceZone);

        final boolean hasDob = dob != null;
        final boolean hasAgeGroup = (ageInYears != null) || (ageInMonths != null) || (ageInDays != null);

        // Enforce exclusivity
        if (hasDob && hasAgeGroup) {
            throw new IllegalArgumentException(
                    "Invalid input: 'dob' is exclusive of 'ageInYears/ageInMonths/ageInDays'. Provide only one group."
            );
        }
        if (!hasDob && !hasAgeGroup) {
            throw new IllegalStateException("Invalid input: 'dob' is exclusive of 'ageInYears/ageInMonths/ageInDays'. Provide only one group.");
        }

        // Validate non-negative ages
        if (ageInYears != null && ageInYears < 0)  throw new IllegalArgumentException("ageInYears cannot be negative.");
        if (ageInMonths != null && ageInMonths < 0) throw new IllegalArgumentException("ageInMonths cannot be negative.");
        if (ageInDays != null && ageInDays < 0)    throw new IllegalArgumentException("ageInDays cannot be negative.");

        if (hasDob) {
            // Convert Date → LocalDate using the device timezone
            LocalDate birthDate = dob.toInstant().atZone(deviceZone).toLocalDate();

            if (birthDate.isAfter(today)) {
                throw new IllegalArgumentException("dob cannot be in the future.");
            }

            return Period.between(birthDate, today).getYears();
        }

        // Derive years from provided age fields (priority: years > months > days)
        if (ageInYears != null) {
            return ageInYears;
        }
        if (ageInMonths != null) {
            return ageInMonths / 12; // floor
        }
        if (ageInDays != null) {
            return ageInDays / 365; // floor (approximation)
        }

        return null; // defensive
    }

    public static int computeAgeInYears(EditText etDob, EditText etAgeYears, EditText etAgeMonths, EditText etAgeDays) {

        Date dob = null;

        try {
            dob = TextUtils.isEmpty(etDob.getText()) ? null :
                    new java.text.SimpleDateFormat("yyyy-MM-dd", java.util.Locale.US)
                            .parse(etDob.getText().toString().trim());
        } catch (java.text.ParseException ignored) { dob = null; }

        Integer years  = TextUtils.isEmpty(etAgeYears.getText())  ? null : Integer.valueOf(etAgeYears.getText().toString().trim());
        Integer months = TextUtils.isEmpty(etAgeMonths.getText()) ? null : Integer.valueOf(etAgeMonths.getText().toString().trim());
        Integer days   = TextUtils.isEmpty(etAgeDays.getText())   ? null : Integer.valueOf(etAgeDays.getText().toString().trim());

        return computeAgeInYearsUsingDeviceZone(dob, years, months, days);

    }
}
