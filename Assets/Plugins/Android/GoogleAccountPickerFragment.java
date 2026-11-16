package com.HungNe.VaelstriderRuinbound;

import android.app.Activity;
import android.app.Fragment;
import android.accounts.Account;
import android.accounts.AccountManager;
import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import com.unity3d.player.UnityPlayer;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;

/**
 * Fragment native vô hình (Headless Fragment) để kích hoạt popup chọn tài khoản Google (Bottom Sheet)
 * Sử dụng Google Play Services Auth (GoogleSignIn) khi có sẵn để lấy Email + Tên + Avatar URL.
 * Tự động fallback về AccountManager của hệ điều hành Android nếu Google Play Services không khả dụng.
 */
public class GoogleAccountPickerFragment extends Fragment {
    private static final String TAG = "GoogleAccountPicker";
    private static final int REQ_GOOGLE_SIGN_IN = 9020;
    private static final int REQ_CHOOSE_ACCOUNT = 9021;
    private static GooglePickerListener sListener = null;

    public interface GooglePickerListener {
        void onSuccess(String email, String displayName, String photoUrl);
        void onCanceled(String reason);
        void onError(String error);
    }

    public static void setListener(GooglePickerListener listener) {
        sListener = listener;
    }

    public static void showPicker(final Activity activity) {
        if (activity == null) {
            notifyError("Activity is null");
            return;
        }
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    GoogleAccountPickerFragment fragment = new GoogleAccountPickerFragment();
                    activity.getFragmentManager()
                            .beginTransaction()
                            .add(fragment, TAG)
                            .commitAllowingStateLoss();
                } catch (Exception e) {
                    Log.e(TAG, "Error adding fragment: " + e.getMessage(), e);
                    notifyError("Failed to launch picker: " + e.getMessage());
                }
            }
        });
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setRetainInstance(true);

        // 1. Thử gọi Google Play Services Auth (Google One-Tap / Bottom Sheet như ảnh)
        if (tryLaunchGoogleSignIn()) {
            return;
        }

        // 2. Fallback sang AccountManager của hệ thống Android
        launchSystemAccountChooser();
    }

    private boolean tryLaunchGoogleSignIn() {
        try {
            Activity act = getActivity();
            if (act == null) return false;

            Class<?> gsoClass = Class.forName("com.google.android.gms.auth.api.signin.GoogleSignInOptions");
            Field defaultSignInField = gsoClass.getField("DEFAULT_SIGN_IN");
            Object defaultSignIn = defaultSignInField.get(null);

            Class<?> builderClass = Class.forName("com.google.android.gms.auth.api.signin.GoogleSignInOptions$Builder");
            Constructor<?> builderConstructor = builderClass.getConstructor(gsoClass);
            Object builderObj = builderConstructor.newInstance(defaultSignIn);

            Method reqEmail = builderClass.getMethod("requestEmail");
            reqEmail.invoke(builderObj);

            Method reqProfile = builderClass.getMethod("requestProfile");
            reqProfile.invoke(builderObj);

            Method buildMethod = builderClass.getMethod("build");
            Object gsoObj = buildMethod.invoke(builderObj);

            Class<?> googleSignInClass = Class.forName("com.google.android.gms.auth.api.signin.GoogleSignIn");
            Method getClientMethod = googleSignInClass.getMethod("getClient", Activity.class, gsoClass);
            Object clientObj = getClientMethod.invoke(null, act, gsoObj);

            Method getSignInIntentMethod = clientObj.getClass().getMethod("getSignInIntent");
            Intent signInIntent = (Intent) getSignInIntentMethod.invoke(clientObj);

            if (signInIntent != null) {
                startActivityForResult(signInIntent, REQ_GOOGLE_SIGN_IN);
                return true;
            }
        } catch (Throwable t) {
            Log.w(TAG, "Google Play Services GoogleSignIn not available or init failed: " + t.getMessage());
        }
        return false;
    }

    private void launchSystemAccountChooser() {
        try {
            Intent intent = AccountManager.newChooseAccountIntent(
                    (Account) null,
                    (java.util.List<Account>) null,
                    new String[]{"com.google"},
                    (String) null,
                    (String) null,
                    (String[]) null,
                    (android.os.Bundle) null
            );

            if (intent != null) {
                startActivityForResult(intent, REQ_CHOOSE_ACCOUNT);
            } else {
                notifyError("Could not create AccountChooser Intent");
                removeSelf();
            }
        } catch (Exception e) {
            Log.e(TAG, "Error starting system account chooser: " + e.getMessage(), e);
            notifyError(e.getMessage());
            removeSelf();
        }
    }

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQ_GOOGLE_SIGN_IN) {
            if (resultCode == Activity.RESULT_OK && data != null) {
                try {
                    Class<?> googleSignInClass = Class.forName("com.google.android.gms.auth.api.signin.GoogleSignIn");
                    Method getSignedInAccountMethod = googleSignInClass.getMethod("getSignedInAccountFromIntent", Intent.class);
                    Object taskObj = getSignedInAccountMethod.invoke(null, data);

                    Class<?> taskClass = Class.forName("com.google.android.gms.tasks.Task");
                    Method isSuccessfulMethod = taskClass.getMethod("isSuccessful");
                    boolean isSuccess = (Boolean) isSuccessfulMethod.invoke(taskObj);

                    if (isSuccess) {
                        Method getResultMethod = taskClass.getMethod("getResult", Class.class);
                        Class<?> apiExceptionClass = Class.forName("com.google.android.gms.common.api.ApiException");
                        Object accountObj = getResultMethod.invoke(taskObj, apiExceptionClass);

                        String email = "";
                        String displayName = "";
                        String photoUrl = "";

                        try {
                            Method getEmailMethod = accountObj.getClass().getMethod("getEmail");
                            Object res = getEmailMethod.invoke(accountObj);
                            if (res != null) email = res.toString();
                        } catch (Exception ignored) {}

                        try {
                            Method getDisplayNameMethod = accountObj.getClass().getMethod("getDisplayName");
                            Object res = getDisplayNameMethod.invoke(accountObj);
                            if (res != null) displayName = res.toString();
                        } catch (Exception ignored) {}

                        try {
                            Method getPhotoUrlMethod = accountObj.getClass().getMethod("getPhotoUrl");
                            Object uriObj = getPhotoUrlMethod.invoke(accountObj);
                            if (uriObj != null) photoUrl = uriObj.toString();
                        } catch (Exception ignored) {}

                        if (email != null && !email.trim().isEmpty()) {
                            notifySuccess(email.trim(), displayName != null ? displayName.trim() : "", photoUrl != null ? photoUrl.trim() : "");
                            removeSelf();
                            return;
                        }
                    }
                } catch (Throwable t) {
                    Log.w(TAG, "Failed to parse GoogleSignIn result: " + t.getMessage(), t);
                }
            }

            if (resultCode == Activity.RESULT_CANCELED) {
                notifyCanceled("User canceled account picker");
                removeSelf();
                return;
            }

            // Nếu Google Play Services fail, thử fallback về AccountManager
            launchSystemAccountChooser();
            return;
        }

        if (requestCode == REQ_CHOOSE_ACCOUNT) {
            if (resultCode == Activity.RESULT_OK && data != null) {
                String accountName = data.getStringExtra(AccountManager.KEY_ACCOUNT_NAME);
                if (accountName != null && !accountName.trim().isEmpty()) {
                    notifySuccess(accountName.trim(), "", "");
                } else {
                    notifyCanceled("No account selected");
                }
            } else {
                notifyCanceled("User canceled account picker");
            }
            removeSelf();
        }
    }

    private static void notifySuccess(final String email, final String displayName, final String photoUrl) {
        if (sListener != null) {
            try {
                sListener.onSuccess(email, displayName, photoUrl);
            } catch (Exception ignored) {}
        }
        try {
            String json = "{\"email\":\"" + escapeJson(email) + "\",\"displayName\":\"" + escapeJson(displayName) + "\",\"photoUrl\":\"" + escapeJson(photoUrl) + "\"}";
            UnityPlayer.UnitySendMessage("PlayFabAuthModalRoot", "OnNativeGooglePickerSuccessJson", json);
        } catch (Exception ignored) {}
    }

    private static String escapeJson(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\").replace("\"", "\\\"").replace("\n", "\\n").replace("\r", "\\r");
    }

    private static void notifyCanceled(final String reason) {
        if (sListener != null) {
            try {
                sListener.onCanceled(reason);
            } catch (Exception ignored) {}
        }
        try {
            UnityPlayer.UnitySendMessage("PlayFabAuthModalRoot", "OnNativeGooglePickerCanceled", reason != null ? reason : "");
        } catch (Exception ignored) {}
    }

    private static void notifyError(final String err) {
        if (sListener != null) {
            try {
                sListener.onError(err);
            } catch (Exception ignored) {}
        }
        try {
            UnityPlayer.UnitySendMessage("PlayFabAuthModalRoot", "OnNativeGooglePickerFailed", err != null ? err : "");
        } catch (Exception ignored) {}
    }

    private void removeSelf() {
        try {
            if (getActivity() != null && getFragmentManager() != null) {
                getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
            }
        } catch (Exception ignored) {}
    }
}
