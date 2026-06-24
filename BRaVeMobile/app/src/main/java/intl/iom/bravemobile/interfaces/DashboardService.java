package intl.iom.bravemobile.interfaces;

import intl.iom.bravemobile.models.activities.RegistrationActivity;
import intl.iom.bravemobile.models.dashboard.CoreDashboardView;

public interface DashboardService {

    CoreDashboardView getViewData(RegistrationActivity activity);
}
