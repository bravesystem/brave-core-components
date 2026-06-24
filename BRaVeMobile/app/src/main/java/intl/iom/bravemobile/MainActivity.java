package intl.iom.bravemobile;

import com.neurotec.core.multibiometric.multimodal.MultiModalActivity;
import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;
import com.neurotec.core.multibiometric.brave.ClientBiometricFlow; //vendor specific

import androidx.activity.result.ActivityResultLauncher;
import androidx.activity.result.contract.ActivityResultContracts;
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.app.AppCompatDelegate;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import android.content.ActivityNotFoundException;
import android.content.Intent;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.net.Uri;
import android.os.Bundle;
import android.provider.Settings;
import android.widget.Button;
import android.widget.Toast;

import java.util.ArrayList;
import java.util.List;

import android.Manifest;

import intl.iom.bravemobile.ui.SplashScreen;

public class MainActivity extends AppCompatActivity {

    //private final BiometricFlow flow = new ClientBiometricFlow();

    //private  ActivityResultLauncher<Intent> biometricLauncher;

    //private ActivityResultLauncher<String[]> requestPermissionsLauncher;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        // Force Light Theme
        AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO);

        setContentView(R.layout.activity_main);

        startActivity(new Intent(this, SplashScreen.class));
        // Remove MainActivity from back stack so Back won't return here
        finish();

    }



}