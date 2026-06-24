package intl.iom.bravemobile.services;

import android.content.Context;
import android.content.SharedPreferences;
import android.provider.Settings;
import android.util.Log;
import android.widget.Toast;

import androidx.preference.PreferenceManager;
import androidx.security.crypto.EncryptedSharedPreferences;
import androidx.security.crypto.MasterKey;
import androidx.security.crypto.MasterKeys;

import java.io.IOException;
import java.security.GeneralSecurityException;

import intl.iom.bravemobile.helpers.StringUtils;

public final class SecureStore {

    private static String LOG = SecureStore.class.getSimpleName();

    private final SharedPreferences prefs;

    public SecureStore(Context ctx) {
        SharedPreferences tempPrefs;
        try {
            // Use a consistent alias for the MasterKey
            MasterKey masterKey = new MasterKey.Builder(ctx, MasterKey.DEFAULT_MASTER_KEY_ALIAS)
                    .setKeyScheme(MasterKey.KeyScheme.AES256_GCM)
                    .build();

            // Use a fixed name for EncryptedSharedPreferences
            tempPrefs = EncryptedSharedPreferences.create(
                    ctx,
                    "secure_store", // Must remain constant across app restarts
                    masterKey,
                    EncryptedSharedPreferences.PrefKeyEncryptionScheme.AES256_SIV,
                    EncryptedSharedPreferences.PrefValueEncryptionScheme.AES256_GCM
            );

            Log.d("SecureStore", "Encrypted storage initialized successfully.");
        } catch (GeneralSecurityException | IOException e) {
            // Fallback only if encryption fails
            Log.e("SecureStore", "Secure storage not available, fallback to legacy store.", e);
            tempPrefs = ctx.getSharedPreferences("legacy_store", Context.MODE_PRIVATE);
        }

        this.prefs = tempPrefs;
    }

    /*public void setServerPublicKey(String base64Key){
        prefs.edit().putString("server_public_key", base64Key).apply();
    }*/
    public void setServerPublicKey(String env,String base64Key)
    {
        if(StringUtils.isBlank(env))
            env = "prod";

        String _env  = env.toLowerCase();

        switch (_env){
            case "uat":
                setServerPkUat(base64Key);
                break;
            case "dev":
                setServerPkDev(base64Key);
                break;
            case "ptr":
                setServerPkPtr(base64Key);
                break;
            default:
                setServerPkProd(base64Key);
                break;
        }
    }

    private void setServerPkDev(String base64Key){
        prefs.edit().putString("server_public_key_dev", base64Key).apply();
    }

    private void setServerPkUat(String base64Key){
        prefs.edit().putString("server_public_key_uat", base64Key).apply();
    }

    private void setServerPkProd(String base64Key){
        prefs.edit().putString("server_public_key_prod", base64Key).apply();
    }

    private void setServerPkPtr(String base64Key){
        prefs.edit().putString("server_public_key_ptr", base64Key).apply();
    }


    /*public String getServerPublicKey()
    {
        return prefs.getString("server_public_key", null);
    }*/

    public String getServerPublicKey()
    {
        String _env  = getEnvironment();

        switch (_env){
            case "uat":
                return prefs.getString("server_public_key_uat", null);
            case "dev":
                return prefs.getString("server_public_key_dev", null);
            case "ptr":
                return prefs.getString("server_public_key_ptr", null);
            default:
                return prefs.getString("server_public_key_prod", null);
        }

    }
    public void setTenantId(String env, int tenantId) {

        if(StringUtils.isBlank(env))
            env = "prod";

        String _env  = env.toLowerCase();

        switch (_env){
            case "dev":
                setTenantId_Dev(tenantId);
                break;
            case "uat":
                setTenantId_Uat(tenantId);
                break;
            case "ptr":
                setTenantId_Ptr(tenantId);
                break;
            default:
                setTenantId_Prod(tenantId);
                break;
        }
    }

    private void setTenantId_Dev(int tenantId) {
        prefs.edit().putInt("tenantId_dev", tenantId).apply();
    }

    private void setTenantId_Uat(int tenantId) {
        prefs.edit().putInt("tenantId_uat", tenantId).apply();
    }

    private void setTenantId_Prod(int tenantId) {
        prefs.edit().putInt("tenantId_prod", tenantId).apply();
    }

    private void setTenantId_Ptr(int tenantId) {
        prefs.edit().putInt("tenantId_ptr", tenantId).apply();
    }

    public boolean isProvisioned(String env)
    {
        String _env  = env.toLowerCase();

        switch (_env){
            case "uat":
                return prefs.getBoolean("uat_provisioned", false);
            case "dev":
                return prefs.getBoolean("dev_provisioned", false);
            case "ptr":
                return prefs.getBoolean("ptr_provisioned", false);
            default:
                return prefs.getBoolean("prod_provisioned", false);
        }

    }

    public void setProvisioned(String env, boolean value)
    {
        if(StringUtils.isBlank(env))
            env = "prod";

        String _env  = env.toLowerCase();

        switch (_env){
            case "uat":
                prefs.edit().putBoolean("uat_provisioned", value).apply();
                break;
            case "dev":
                prefs.edit().putBoolean("dev_provisioned", value).apply();
                break;
            case "ptr":
                prefs.edit().putBoolean("ptr_provisioned", value).apply();
                break;
            default:
                prefs.edit().putBoolean("prod_provisioned", value).apply();
                break;
        }

    }

    public boolean isDeviceVerified()  {
        return prefs.getBoolean("integrity_verified", false);
    }
    public void setDeviceVerified(boolean isIntegrityVerified) {
        prefs.edit().putBoolean("integrity_verified", isIntegrityVerified).apply();
    }

    public void setDeviceId(String deviceId) { prefs.edit().putString("brave_esid", deviceId).apply(); }
    public String getDeviceId() { return prefs.getString("brave_esid", null); }

    public void setHouseholdPrefix(String env, String prefix) {

        if(StringUtils.isBlank(env))
            env = "prod";

        String _env  = env.toLowerCase();

        switch (_env){
            case "uat":
                setHouseholdPrefix_Uat( prefix);
                break;
            case "dev":
                setHouseholdPrefix_Dev( prefix);
                break;
            case "ptr":
                setHouseholdPrefix_Ptr( prefix);
                break;
            default:
                setHouseholdPrefix_Prod( prefix);
                break;
        }

    }
    private  void setHouseholdPrefix_Dev( String prefix) {
        prefs.edit().putString("household_prefix_dev", prefix).apply();

    }
    private  void setHouseholdPrefix_Uat( String prefix) {
        prefs.edit().putString("household_prefix_uat", prefix).apply();

    }
    private  void setHouseholdPrefix_Prod( String prefix) {
        prefs.edit().putString("household_prefix_prod", prefix).apply();

    }
    private  void setHouseholdPrefix_Ptr( String prefix) {
        prefs.edit().putString("household_prefix_ptr", prefix).apply();

    }
    public String getHouseholdPrefix() {

        String _env  = getEnvironment();

        switch (_env){
            case "uat":
                return prefs.getString("household_prefix_uat", null);
            case "dev":
                return prefs.getString("household_prefix_dev", null);
            case "ptr":
                return prefs.getString("household_prefix_ptr", null);
            default:
                return prefs.getString("household_prefix_prod", null);
        }
    }


    public void setProdUrl(String url){
         prefs.edit().putString("prod", url).apply();
    }
    public void setPartnerUrl(String url){
         prefs.edit().putString("ptr", url).apply();
    }
    private String getProdUrl(){
        return prefs.getString("prod", null);
    }

    private String getPartnerUrl(){
        return prefs.getString("ptr", null);
    }
    public void setUatUrl(String url){
        prefs.edit().putString("uat", url).apply();
    }
    private String getUatUrl(){
        return prefs.getString("uat", null);
    }

    public void setDevUrl(String url){
        prefs.edit().putString("dev", url).apply();
    }

    public void setDevUrlTesting(String url){
        prefs.edit().putString("dev", url).commit();
    }

    private String getDevUrl(){
        return prefs.getString("dev", null);
    }

    // Tokens
    public void saveTokens(String env, String access, String refresh) {

        if(StringUtils.isBlank(env))
            env = "prod";

        String _env  = env.toLowerCase();

        switch (_env){
            case "uat":
                saveTokens_Uat(access, refresh);
                break;
            case "dev":
                saveTokens_Dev(access, refresh);
                break;
            case "ptr":
                saveTokens_Ptr(access, refresh);
                break;
            default:
                saveTokens_Prod(access, refresh);
                break;
        }
    }

    private void saveTokens_Dev(String access, String refresh) {
        prefs.edit().putString("access_dev", access).putString("refresh_dev", refresh).apply();
    }

    private void saveTokens_Uat(String access, String refresh) {
        prefs.edit().putString("access_uat", access).putString("refresh_uat", refresh).apply();
    }

    private void saveTokens_Prod(String access, String refresh) {
        prefs.edit().putString("access_prod", access).putString("refresh_prod", refresh).apply();
    }

    private void saveTokens_Ptr(String access, String refresh) {
        prefs.edit().putString("access_ptr", access).putString("refresh_ptr", refresh).apply();
    }


    public String getAccess()  {

        String _env  = getEnvironment();

        switch (_env){
            case "uat":
                return prefs.getString("access_uat", null);
            case "dev":
                return prefs.getString("access_dev", null);
            case "ptr":
                return prefs.getString("access_ptr", null);
            default:
                return prefs.getString("access_prod", null);
        }

    }
    public String getRefresh() {

        String _env  = getEnvironment();

        switch (_env){
            case "uat":
                return prefs.getString("refresh_uat", null);
            case "dev":
                return prefs.getString("refresh_dev", null);
            case "ptr":
                return prefs.getString("refresh_ptr", null);
            default:
                return prefs.getString("refresh_prod", null);
        }
    }

    // Other secured params
    /*public void setTenantId(int tenantId) {
        prefs.edit().putInt("tenantId", tenantId).apply();
    }*/
    public int getTenantId() {

        switch (getEnvironment()) {
            case "dev":
                return prefs.getInt("tenantId_dev", 0);
            case "uat":
                return prefs.getInt("tenantId_uat", 0);
            case "ptr":
                return prefs.getInt("tenantId_ptr", 0);
            default:
                return prefs.getInt("tenantId_prod", 0);
        }

    }
    public void setEnvironment(String env) {

        if(env==null || env.isEmpty())
        {
            prefs.edit().putString("env", "prod").apply();
            return;
        }

        String _env  = env.toLowerCase();

        switch (_env){
            case "uat":
            case "dev":
            case "ptr":
                prefs.edit().putString("env", _env).apply();
                break;
            default:
                prefs.edit().putString("env", "prod").apply();
                break;
        }

    }
    public String getEnvironment() { return prefs.getString("env", "prod"); }

    public String getCurrentUrl()
    {
        switch (getEnvironment()) {
            case "dev":
                return getDevUrl();
            case "uat":
                return getUatUrl();
            case "ptr":
                return getPartnerUrl();
            default:
                return getProdUrl();
        }
    }

    public String getEndpoint(String env) {

        if(StringUtils.isBlank(env))
            env = "prod";

        String _env  = env.toLowerCase();

        switch (_env) {
            case "dev":
                return getDevUrl();
            case "uat":
                return getUatUrl();
            default:
                return getProdUrl();
        }
    }

    // Byte[] helper (store as base64url)
    /*public void putBytes(String key, byte[] value) {
        String b64u = java.util.Base64.getUrlEncoder().withoutPadding().encodeToString(value);
        prefs.edit().putString(key, b64u).apply();
    }
    public byte[] getBytes(String key) {
        String s = prefs.getString(key, null);
        return s == null ? null : java.util.Base64.getUrlDecoder().decode(s);
    }*/

    public void invalidateJws() {
        prefs.edit().remove("access").apply();
        //prefs.edit().remove("refresh").apply();
    }

    public void login(String  enumerator) { prefs.edit().putString("enumerator", enumerator).apply(); }
    public String getEnumerator() { return prefs.getString("enumerator", "x0" ); }
    public void logout(){
        prefs.edit().remove("enumerator").apply();
    }


}
