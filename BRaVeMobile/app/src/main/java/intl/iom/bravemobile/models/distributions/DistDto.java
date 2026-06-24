package intl.iom.bravemobile.models.distributions;

import java.util.ArrayList;
import java.util.List;

public class DistDto {
    public int id;
    public String activityCode;
    public String title;
    public int type;
    public boolean biometricReceiptRequired ;
    public boolean photoReceiptRequired ;
    public String additionalInfo;
    public List<DistItem> items;

    public int mode; //1- online, 2- offline, 3- hybrid

    public DistDto(Distribution distribution) {

        id = distribution.distributionId;
        title = distribution.title;
        type = distribution.type;
        mode = distribution.mode;
        this.biometricReceiptRequired = distribution.biometricReceiptRequired;
        photoReceiptRequired = distribution.photoReceiptRequired;
        additionalInfo = distribution.additionalInfo;
        items = getAllItems(distribution);

    }

    public boolean allowDownload(){
        return mode==1 || mode==3;
    }

    private  List<DistItem> getAllItems(Distribution distribution) {
        List<DistItem> result = new ArrayList<>();

        if (distribution == null || distribution.kits == null) {
            return result;
        }

        for (Kit kit : distribution.kits) {
            if (kit != null && kit.items != null) {
                result.addAll(kit.items);
            }
        }

        return result;
    }

}
