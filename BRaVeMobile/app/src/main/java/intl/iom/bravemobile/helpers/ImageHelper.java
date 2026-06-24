package intl.iom.bravemobile.helpers;

import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.os.Looper;
import android.util.Base64;
import android.view.View;
import android.widget.ImageView;

public final class ImageHelper {

    /**
     * Convert Base64 string (optionally with data URI prefix) to a downscaled Bitmap.
     * @param base64String The base64 image
     * @param reqWidth  target max width in pixels (to avoid OOM)
     * @param reqHeight target max height in pixels (to avoid OOM)
     * @return Bitmap or null if decoding fails
     */
    public static Bitmap decodeBase64(String base64String, int reqWidth, int reqHeight)
    {

        if (base64String == null || base64String.trim().isEmpty())
            return null;

        try {
            // Remove optional prefix like "data:image/jpeg;base64,"
            String clean = stripDataUriPrefix(base64String);

            // Decode to bytes
            byte[] bytes = Base64.decode(clean, Base64.DEFAULT);

            // 1st pass: only get dimensions
            BitmapFactory.Options options = new BitmapFactory.Options();
            options.inJustDecodeBounds = true;
            BitmapFactory.decodeByteArray(bytes, 0, bytes.length, options);

            // Downsample
            options.inSampleSize = calculateInSampleSize(options, reqWidth, reqHeight);

            // 2nd pass: decode bitmap
            options.inJustDecodeBounds = false;
            options.inPreferredConfig = Bitmap.Config.RGB_565;

            return BitmapFactory.decodeByteArray(bytes, 0, bytes.length, options);
        } catch (Exception e) {
            return null;
        }
    }

    // Remove data URI prefix if exists
    public static String stripDataUriPrefix(String base64) {
        int commaIndex = base64.indexOf(",");
        if (commaIndex > 0 && base64.substring(0, commaIndex).contains("base64")) {
            return base64.substring(commaIndex + 1);
        }
        return base64;
    }

    public static int dpToPx(View v, int dp) {
        float d = v.getResources().getDisplayMetrics().density;
        return Math.max(1, (int) (dp * d + 0.5f));
    }
    // Calculate downscale ratio
    public static int calculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight) {
        int height = options.outHeight;
        int width = options.outWidth;
        int inSampleSize = 1;

        if (height > reqHeight || width > reqWidth) {
            final int halfHeight = height / 2;
            final int halfWidth = width / 2;

            while ((halfHeight / inSampleSize) >= reqHeight &&
                    (halfWidth / inSampleSize) >= reqWidth) {
                inSampleSize *= 2;
            }
        }
        return inSampleSize;
    }


    public static void setBase64Image(ImageView imageView, String base64) {
        if (imageView == null || base64 == null || base64.trim().isEmpty()) return;

        // Strip data URI prefix if present
        int comma = base64.indexOf(',');
        if (comma >= 0) base64 = base64.substring(comma + 1);

        try {
            byte[] bytes = Base64.decode(base64, Base64.DEFAULT);

            // (Optional) scale down to ImageView size to reduce memory
            BitmapFactory.Options opts = new BitmapFactory.Options();
            opts.inPreferredConfig = Bitmap.Config.ARGB_8888;
            Bitmap bmp = BitmapFactory.decodeByteArray(bytes, 0, bytes.length, opts);

            // Ensure we set the image on the main thread
            if (Looper.myLooper() == Looper.getMainLooper()) {
                imageView.setImageBitmap(bmp);
            } else {
                imageView.post(() -> imageView.setImageBitmap(bmp));
            }
        } catch (IllegalArgumentException e) {
            // Invalid base64
            e.printStackTrace();
        }
    }

}
