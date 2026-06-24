package intl.iom.bravemobile.models.registrations;

import java.util.List;

import intl.iom.bravemobile.models.DataPacketHeader;
import intl.iom.bravemobile.models.activities.BiometricCheckModel;

public class VerifyTemplatesRequest
{
    public DataPacketHeader header ;
    public String templates;  //encrypted Templates
}
