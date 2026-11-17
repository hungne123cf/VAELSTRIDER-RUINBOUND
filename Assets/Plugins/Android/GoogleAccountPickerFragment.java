package com.HungNe.VaelstriderRuinbound;

import android.app.Activity;
import android.app.Fragment;
import android.app.FragmentManager;
import android.accounts.Account;
import android.accounts.AccountManager;
import android.content.Intent;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import com.unity3d.player.UnityPlayer;
import java.lang.reflect.Constructor;
import java.lang.reflect.Field;
import java.lang.reflect.Method;
import java.util.ArrayList;

/**
 * Fragment native vô hình (Headless Fragment) để kích hoạt popup chọn tài khoản Google trên Android.
 * Ưu tiên sử dụng AccountManager.newChooseAccountIntent (hộp thoại chọn tài khoản Google gốc của hệ thống Android / Google Play Services)
 * vì hoạt động 100% trên mọi bản build APK (kể cả Debug / Custom Keystore mà không bị lỗi DEVELOPER_ERROR 10 do thiếu SHA-1 trên Google Cloud).
 * Đồng thời tự động trích xuất thêm DisplayName & PhotoUrl từ GoogleSignIn cache nếu có.
 */
public class GoogleAccountPickerFragment extends Fragment {
    private static final String TAG = "GoogleAccountPicker";
    private static final int REQ_CHOOSE_ACCOUNT = 9021;
    private static final int REQ_GOOGLE_SIGN_IN = 9020;
    private static GooglePickerListener sListener = null;
    private boolean mHasLaunchedPicker = false;

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
                    FragmentManager fm = activity.getFragmentManager();
                    Fragment existing = fm.findFragmentByTag(TAG);
                    if (existing != null) {
                        fm.beginTransaction().remove(existing).commitAllowingStateLoss();
                        fm.executePendingTransactions();
                    }

                    GoogleAccountPickerFragment fragment = new GoogleAccountPickerFragment();
                    fm.beginTransaction()
                            .add(fragment, TAG)
                            .commitAllowingStateLoss();
                    fm.executePendingTransactions();
                } catch (Throwable e) {
                    Log.e(TAG, "Error adding fragment: " + e.getMessage(), e);
                    notifyError("Không thể mở hộp thoại Google: " + e.getMessage());
                }
            }
        });
    }

    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setRetainInstance(true);

        if (!mHasLaunchedPicker) {
            mHasLaunchedPicker = true;
            // Ưu tiên số 1: Thử Google Sign-In chuẩn để lấy trọn vẹn Tên thật + Avatar URL
            if (tryLaunchGoogleSignIn()) {
                return;
            }

            // Fallback số 2: Mở hộp thoại chọn tài khoản Google gốc của Android (AccountManager) nếu GoogleSignIn không khởi tạo được
            if (launchSystemAccountChooser()) {
                return;
            }

            notifyError("Thiết bị không hỗ trợ hộp thoại chọn tài khoản Google.");
            removeSelf();
        }
    }

    @SuppressWarnings("deprecation")
    private boolean launchSystemAccountChooser() {
        try {
            Intent intent = null;
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                intent = AccountManager.newChooseAccountIntent(
                        (Account) null,
                        (java.util.List<Account>) null,
                        new String[]{"com.google"},
                        (String) null,
                        (String) null,
                        (String[]) null,
                        (Bundle) null
                );
            } else {
                intent = AccountManager.newChooseAccountIntent(
                        (Account) null,
                        (ArrayList<Account>) null,
                        new String[]{"com.google"},
                        true,
                        (String) null,
                        (String) null,
                        (String[]) null,
                        (Bundle) null
                );
            }

            if (intent != null) {
                startActivityForResult(intent, REQ_CHOOSE_ACCOUNT);
                return true;
            }
        } catch (Throwable e) {
            Log.e(TAG, "Error starting system account chooser: " + e.getMessage(), e);
        }
        return false;
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

            // Bổ sung requestIdToken để lấy đầy đủ Profile và Photo URL
            try {
                String webClientId = "397249070493-tdjj1fnblridf17atv36jat90vju5n0v.apps.googleusercontent.com";
                Method reqIdToken = builderClass.getMethod("requestIdToken", String.class);
                reqIdToken.invoke(builderObj, webClientId);
            } catch (Throwable ignored) {}

            Method buildMethod = builderClass.getMethod("build");
            Object gsoObj = buildMethod.invoke(builderObj);

            Class<?> googleSignInClass = Class.forName("com.google.android.gms.auth.api.signin.GoogleSignIn");
            Method getClientMethod = googleSignInClass.getMethod("getClient", Activity.class, gsoClass);
            final Object clientObj = getClientMethod.invoke(null, act, gsoObj);

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

    @Override
    public void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQ_CHOOSE_ACCOUNT) {
            if (resultCode == Activity.RESULT_OK && data != null) {
                String accountName = data.getStringExtra(AccountManager.KEY_ACCOUNT_NAME);
                if (accountName != null && !accountName.trim().isEmpty()) {
                    String cleanEmail = accountName.trim();
                    String displayName = "";
                    String photoUrl = "";

                    // Thử lấy thêm tên hiển thị và ảnh đại diện từ GoogleSignIn nếu tài khoản trùng khớp
                    try {
                        Activity act = getActivity();
                        if (act != null) {
                            Class<?> googleSignInClass = Class.forName("com.google.android.gms.auth.api.signin.GoogleSignIn");
                            Method getLastMethod = googleSignInClass.getMethod("getLastSignedInAccount", android.content.Context.class);
                            Object lastAcc = getLastMethod.invoke(null, act);
                            if (lastAcc != null) {
                                Method getEmailMethod = lastAcc.getClass().getMethod("getEmail");
                                Object lastEmailObj = getEmailMethod.invoke(lastAcc);
                                String lastEmail = lastEmailObj != null ? lastEmailObj.toString().trim() : "";
                                if (lastEmail.isEmpty() || lastEmail.equalsIgnoreCase(cleanEmail)) {
                                    Method getDisplayNameMethod = lastAcc.getClass().getMethod("getDisplayName");
                                    Object dispObj = getDisplayNameMethod.invoke(lastAcc);
                                    if (dispObj != null) displayName = dispObj.toString().trim();

                                    Method getPhotoUrlMethod = lastAcc.getClass().getMethod("getPhotoUrl");
                                    Object uriObj = getPhotoUrlMethod.invoke(lastAcc);
                                    if (uriObj != null) photoUrl = uriObj.toString().trim();
                                }
                            }
                        }
                    } catch (Throwable ignored) {}

                    notifySuccess(cleanEmail, displayName, photoUrl);
                } else {
                    notifyError("Không nhận được địa chỉ Email từ tài khoản đã chọn.");
                }
            } else {
                notifyCanceled("Người dùng đã đóng bảng chọn tài khoản Google.");
            }
            removeSelf();
            return;
        }

        if (requestCode == REQ_GOOGLE_SIGN_IN) {
            // Lưu ý: GoogleSignIn trả về lỗi (như DEVELOPER_ERROR 10) ngay cả khi resultCode == RESULT_CANCELED
            if (data != null) {
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
                    } else {
                        // Kiểm tra mã lỗi ApiException
                        Method getExceptionMethod = taskClass.getMethod("getException");
                        Object exObj = getExceptionMethod.invoke(taskObj);
                        if (exObj != null) {
                            try {
                                Method getStatusCodeMethod = exObj.getClass().getMethod("getStatusCode");
                                int statusCode = (Integer) getStatusCodeMethod.invoke(exObj);
                                Log.w(TAG, "GoogleSignIn failed with status code: " + statusCode);
                                if (statusCode != 12501 && statusCode != 16) {
                                    // Lỗi cấu hình SHA-1 (code 10 / 12500): Chuyển sang AccountManager
                                    if (launchSystemAccountChooser()) {
                                        return;
                                    }
                                    notifyError("Google Sign-In lỗi mã " + statusCode + " (SHA-1 OAuth)");
                                    removeSelf();
                                    return;
                                }
                            } catch (Throwable ignored) {}
                        }
                    }
                } catch (Throwable t) {
                    Log.w(TAG, "Failed to parse GoogleSignIn result: " + t.getMessage(), t);
                }
            }

            notifyCanceled("Người dùng đã đóng bảng chọn tài khoản Google.");
            removeSelf();
        }
    }

    private static void notifySuccess(final String email, final String displayName, final String photoUrl) {
        GooglePickerListener listener = sListener;
        sListener = null;
        boolean handledByProxy = false;
        if (listener != null) {
            try {
                listener.onSuccess(email, displayName, photoUrl);
                handledByProxy = true;
            } catch (Throwable t) {
                Log.w(TAG, "Proxy listener onSuccess error: " + t.getMessage());
            }
        }
        if (!handledByProxy) {
            try {
                String json = "{\"email\":\"" + escapeJson(email) + "\",\"displayName\":\"" + escapeJson(displayName) + "\",\"photoUrl\":\"" + escapeJson(photoUrl) + "\"}";
                UnityPlayer.UnitySendMessage("PlayFabAuthModalRoot", "OnNativeGooglePickerSuccessJson", json);
            } catch (Throwable ignored) {}
        }
    }

    private static String escapeJson(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\").replace("\"", "\\\"").replace("\n", "\\n").replace("\r", "\\r");
    }

    private static void notifyCanceled(final String reason) {
        GooglePickerListener listener = sListener;
        sListener = null;
        boolean handledByProxy = false;
        if (listener != null) {
            try {
                listener.onCanceled(reason != null ? reason : "");
                handledByProxy = true;
            } catch (Throwable t) {
                Log.w(TAG, "Proxy listener onCanceled error: " + t.getMessage());
            }
        }
        if (!handledByProxy) {
            try {
                UnityPlayer.UnitySendMessage("PlayFabAuthModalRoot", "OnNativeGooglePickerCanceled", reason != null ? reason : "");
            } catch (Throwable ignored) {}
        }
    }

    private static void notifyError(final String err) {
        GooglePickerListener listener = sListener;
        sListener = null;
        boolean handledByProxy = false;
        if (listener != null) {
            try {
                listener.onError(err != null ? err : "Unknown error");
                handledByProxy = true;
            } catch (Throwable t) {
                Log.w(TAG, "Proxy listener onError error: " + t.getMessage());
            }
        }
        if (!handledByProxy) {
            try {
                UnityPlayer.UnitySendMessage("PlayFabAuthModalRoot", "OnNativeGooglePickerFailed", err != null ? err : "Unknown error");
            } catch (Throwable ignored) {}
        }
    }

    private void removeSelf() {
        try {
            if (getActivity() != null && getFragmentManager() != null) {
                getFragmentManager().beginTransaction().remove(this).commitAllowingStateLoss();
            }
        } catch (Throwable ignored) {}
    }
}
