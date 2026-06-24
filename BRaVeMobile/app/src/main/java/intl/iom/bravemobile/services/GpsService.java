package intl.iom.bravemobile.services;

import android.Manifest;
import android.content.Context;
import android.content.pm.PackageManager;
import android.location.Location;
import android.location.LocationManager;

import androidx.core.app.ActivityCompat;

import java.util.Locale;

public class GpsService {

    private final Context context;

    public GpsService(Context context) {
        this.context = context;//.getApplicationContext();
    }

    /**
     * Returns last known coordinates as "lat, lon" (e.g. "12.345678, -45.678901").
     * If permission is missing or no location is available, returns an empty string.
     */
    public String getGpsCoordinates() {

        Location loc = getLastKnownLocation();
        if (loc == null) return "";

        double lat = loc.getLatitude();
        double lon = loc.getLongitude();
        float acc  = loc.hasAccuracy() ? loc.getAccuracy() : -1f;

        if (acc > 0) {
            return String.format(Locale.US,
                    "%.6f, %.6f, ±%.1f m",
                    lat, lon, acc);
        } else {
            return String.format(Locale.US,
                    "%.6f, %.6f",
                    lat, lon);
        }
    }

    private Location getLastKnownLocation() {
        boolean fineOk = ActivityCompat.checkSelfPermission(
                context, Manifest.permission.ACCESS_FINE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED;
        boolean coarseOk = ActivityCompat.checkSelfPermission(
                context, Manifest.permission.ACCESS_COARSE_LOCATION
        ) == PackageManager.PERMISSION_GRANTED;

        if (!fineOk && !coarseOk) return null;

        LocationManager lm = (LocationManager) context.getSystemService(Context.LOCATION_SERVICE);
        if (lm == null) return null;

        Location loc = lm.getLastKnownLocation(LocationManager.GPS_PROVIDER);
        if (loc == null) {
            loc = lm.getLastKnownLocation(LocationManager.NETWORK_PROVIDER);
        }
        return loc;
    }


}
