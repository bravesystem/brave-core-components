package intl.iom.bravemobile.services;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.BitmapFactory;
import android.graphics.Matrix;
import android.net.Uri;
import android.util.Base64;

import androidx.activity.result.ActivityResultCaller;
import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.core.content.FileProvider;
import androidx.exifinterface.media.ExifInterface;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/**
 * Usage:
 *   cameraHelper = new CameraCaptureHelper(this, this);
 *   btnCapture.setOnClickListener(v -> cameraHelper.capture(new CameraCaptureHelper.Listener() { ... }));
 */
public class CameraCaptureHelper {

    public static class Result {
        public final Uri uri;            // content:// uri from FileProvider
        public final String filePath;    // absolute path to the (corrected) file
        public final Bitmap bitmap;      // rotated preview (downsampled)
        public final String base64;      // base64 of preview (JPEG)

        Result(Uri uri, String filePath, Bitmap bitmap, String base64) {
            this.uri = uri;
            this.filePath = filePath;
            this.bitmap = bitmap;
            this.base64 = base64;
        }
    }

    public interface Listener {
        void onCaptured(Result result);
        void onCanceled();
        void onError(Exception e);
    }

    private final Context appContext;
    private final ActivityResultLauncher<Uri> takePictureLauncher;
    private final String authority;

    private File currentFile;
    private Uri currentUri;
    private Listener currentListener;

    /**
     * @param caller  Activity or Fragment (implements ActivityResultCaller)
     * @param context any Context (we keep appContext)
     */
    public CameraCaptureHelper(ActivityResultCaller caller, Context context) {
        this.appContext = context.getApplicationContext();
        this.authority = appContext.getPackageName() + ".fileprovider";

        this.takePictureLauncher =
                caller.registerForActivityResult(new ActivityResultContracts.TakePicture(), success -> {
                    if (currentListener == null) return;

                    if (Boolean.TRUE.equals(success)) {
                        try {
                            handleCaptured();
                        } catch (Exception e) {
                            currentListener.onError(e);
                        } finally {
                            clearState();
                        }
                    } else {
                        currentListener.onCanceled();
                        clearState();
                    }
                });
    }

    public void capture(Listener listener) {
        this.currentListener = listener;
        try {
            currentFile = createTempImageFile(appContext);
            currentUri = FileProvider.getUriForFile(appContext, authority, currentFile);
            takePictureLauncher.launch(currentUri);
        } catch (Exception e) {
            Listener l = currentListener;
            clearState();
            if (l != null) l.onError(e);
        }
    }

    // --- Internal ---

    private void handleCaptured() throws IOException {
        // 1) Decode a preview (downsampled)
        Bitmap bmp = decodeSampled(currentFile.getAbsolutePath(), 1200, 1200);

        // 2) Read EXIF orientation
        ExifInterface exif = new ExifInterface(currentFile.getAbsolutePath());
        int orientation = exif.getAttributeInt(ExifInterface.TAG_ORIENTATION, ExifInterface.ORIENTATION_NORMAL);
        int degrees = exifToDegrees(orientation);

        // 3) Rotate preview
        if (degrees != 0 && bmp != null) {
            bmp = rotateBitmap(bmp, degrees);
        }

        // 4) OPTIONAL: Permanently fix file pixels & normalize EXIF
        //    (so any external viewer will also see it upright)
        if (degrees != 0) {
            rewriteRotatedFile(currentFile, degrees);
            ExifInterface exif2 = new ExifInterface(currentFile.getAbsolutePath());
            exif2.setAttribute(ExifInterface.TAG_ORIENTATION,
                    String.valueOf(ExifInterface.ORIENTATION_NORMAL));
            exif2.saveAttributes();
        }

        // 5) Build base64 from preview bitmap
        String b64 = (bmp != null) ? bitmapToBase64Jpeg(bmp, 85) : null;

        currentListener.onCaptured(new Result(
                currentUri,
                currentFile.getAbsolutePath(),
                bmp,
                b64
        ));
    }

    private void clearState() {
        currentFile = null;
        currentUri = null;
        currentListener = null;
    }

    // --- Utils ---

    private static File createTempImageFile(Context ctx) throws IOException {
        String timeStamp = new SimpleDateFormat("yyyyMMdd_HHmmss", Locale.US).format(new Date());
        File dir = new File(ctx.getCacheDir(), "images");
        if (!dir.exists()) dir.mkdirs();
        return File.createTempFile("IMG_" + timeStamp + "_", ".jpg", dir);
    }

    private static Bitmap decodeSampled(String path, int reqW, int reqH) {
        BitmapFactory.Options opts = new BitmapFactory.Options();
        opts.inJustDecodeBounds = true;
        BitmapFactory.decodeFile(path, opts);
        opts.inSampleSize = calcInSampleSize(opts, reqW, reqH);
        opts.inJustDecodeBounds = false;
        return BitmapFactory.decodeFile(path, opts);
    }

    private static int calcInSampleSize(BitmapFactory.Options opts, int reqW, int reqH) {
        int h = opts.outHeight, w = opts.outWidth, inSample = 1;
        if (h > reqH || w > reqW) {
            int halfH = h / 2, halfW = w / 2;
            while ((halfH / inSample) >= reqH && (halfW / inSample) >= reqW) {
                inSample *= 2;
            }
        }
        return inSample;
    }

    private static int exifToDegrees(int exifOrientation) {
        switch (exifOrientation) {
            case ExifInterface.ORIENTATION_ROTATE_90:  return 90;
            case ExifInterface.ORIENTATION_ROTATE_180: return 180;
            case ExifInterface.ORIENTATION_ROTATE_270: return 270;
            // If you need to handle mirror/transpose cases, add them here.
            default: return 0;
        }
    }

    private static Bitmap rotateBitmap(Bitmap src, int degrees) {
        if (src == null || degrees == 0) return src;
        Matrix m = new Matrix();
        m.postRotate(degrees);
        Bitmap out = Bitmap.createBitmap(src, 0, 0, src.getWidth(), src.getHeight(), m, true);
        if (out != src) src.recycle();
        return out;
    }

    private static String bitmapToBase64Jpeg(Bitmap bmp, int quality) {
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        bmp.compress(Bitmap.CompressFormat.JPEG, quality, baos);
        return Base64.encodeToString(baos.toByteArray(), Base64.NO_WRAP);
    }

    /**
     * Overwrites the file with a rotated version (full size), preserving JPEG quality.
     * Called only when degrees != 0.
     */
    private static void rewriteRotatedFile(File file, int degrees) throws IOException {
        BitmapFactory.Options opts = new BitmapFactory.Options();
        opts.inPreferredConfig = Bitmap.Config.ARGB_8888;
        Bitmap full = BitmapFactory.decodeFile(file.getAbsolutePath(), opts);
        if (full == null) return;

        Matrix m = new Matrix();
        m.postRotate(degrees);
        Bitmap rotated = Bitmap.createBitmap(full, 0, 0, full.getWidth(), full.getHeight(), m, true);
        if (rotated != full) full.recycle();

        try (FileOutputStream fos = new FileOutputStream(file)) {
            rotated.compress(Bitmap.CompressFormat.JPEG, 92, fos);
        }
        rotated.recycle();
    }
}
