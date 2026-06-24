package intl.iom.bravemobile.services.mocks;

import android.os.Build;

import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;

import intl.iom.bravemobile.helpers.SelectItem;
import intl.iom.bravemobile.interfaces.LookupService;
import intl.iom.bravemobile.statics.KnownLkp;

public class MockedLookupService implements LookupService {

    // Store by numeric ID
    private final Map<Integer, List<SelectItem>> byId = new HashMap<>();
    // Store by lowercase name for case-insensitive matches
    private final Map<String, List<SelectItem>> byName = new HashMap<>();

    public MockedLookupService() {

        // Java 8 list of vulnerabilities
        List<SelectItem> vulnerabilities = Arrays.asList(
                new SelectItem(1, "Pregnant"),
                new SelectItem(2, "Lactating mother"),
                new SelectItem(3, "Child under 5"),
                new SelectItem(4, "Elderly (60+)"),
                new SelectItem(5, "Person with disability"),
                new SelectItem(6, "Chronic illness"),
                new SelectItem(7, "Female-headed household"),
                new SelectItem(8, "Single parent / primary caregiver"),
                new SelectItem(9, "Child-headed"),
                new SelectItem(10, "Disability")
        );

        //// 8) Vulnerabilities — SELECT_MANY (lookup: 1=Pregnant, 2=Lactating, 3=Elderly(60+), 4=Child-headed, 5=Disability)

        List<SelectItem> vulnerabilities2 = Arrays.asList(
                new SelectItem(1, "Pregnant"),
                new SelectItem(2, "Lactating mother"),
                new SelectItem(3, "Child under 5"),
                new SelectItem(4, "Elderly (60+)"),
                new SelectItem(5, "Person with disability"),
                new SelectItem(6, "Chronic illness"),
                new SelectItem(7, "Female-headed household"),
                new SelectItem(8, "Single parent / primary caregiver")
        );

        // Dummy datasets
        List<SelectItem> relationships = Arrays.asList(
                new SelectItem(0, "Head of Household"),
                new SelectItem(1, "Spouse"),
                new SelectItem(2, "Son/Daughter"),
                new SelectItem(3, "Mother/Father"),
                new SelectItem(99, "Other")
        );

        // Dummy datasets
        List<SelectItem> household_types = Arrays.asList(
                new SelectItem(1, "IDP"),
                new SelectItem(2, "Host Community"),
                new SelectItem(3, "Refugee"),
                new SelectItem(4, "Returnee")
        );

        // Dummy datasets
        List<SelectItem> activity_types = Arrays.asList(
                new SelectItem(1, "Family"),
                new SelectItem(2, "Individual"),
                new SelectItem(3, "Survey"),
                new SelectItem(4, "Distribution")
        );

        // Dummy datasets
        List<SelectItem> assistance_received = Arrays.asList(
                new SelectItem(1, "Food"),
                new SelectItem(2, "Shelter"),
                new SelectItem(3, "Health services"),
                new SelectItem(4, "Education support"),
                new SelectItem(99, "None")
        );

        List<SelectItem> urgent_needs = Arrays.asList(
                new SelectItem(1, "Food"),
                new SelectItem(2, "Clean water"),
                new SelectItem(3, "Shelter"),
                new SelectItem(4, "Medical care"),
                new SelectItem(5, "Protection/Security")
        );

        List<SelectItem> education_levels = Arrays.asList(
                new SelectItem(1, "No formal education"),
                new SelectItem(2, "Primary school"),
                new SelectItem(3, "Secondary school"),
                new SelectItem(4, "Vocational training"),
                new SelectItem(5, "University or higher"),
                new SelectItem(6, "Not applicable")

        );
//(lookup: 1=Displacement, 2=Disability, 3=Orphanhood, 4=Other)
        List<SelectItem> assistance_reasons = Arrays.asList(
                new SelectItem(1, "Displacement"),
                new SelectItem(2, "Disability"),
                new SelectItem(3, "Orphanhood"),
                new SelectItem(4, "Other")

        );

        //  // 9) Preferred contact — SELECT_ONE (lookup: 1=Phone, 2=SMS, 3=WhatsApp)
        List<SelectItem> contact_types = Arrays.asList(
                new SelectItem(1, "Phone"),
                new SelectItem(2, "SMS"),
                new SelectItem(3, "WhatsApp"),
                new SelectItem(4, "Other")

        );


    }

    @Override
    public void loadAll() {

    }

    @Override
    public Optional<List<SelectItem>> getLookupItemList(int lookupId)
    {
        List<SelectItem> items = byId.get(lookupId);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            return Optional.ofNullable(items);
        }
        return null;
    }

    @Override
    public Optional<List<SelectItem>> getLookupItemList(String lookupName)
    {
        if (lookupName == null) return null;

        List<SelectItem> items = byName.get(lookupName);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            return Optional.ofNullable(items);
        }
        return null;
    }

}
