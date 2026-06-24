# Integrating an Alternative Biometric Vendor

This guide explains how to replace the default **Neurotechnology** integration with another biometric SDK while keeping the rest of the BRaVe Android app unchanged.

The app talks to biometrics only through the `BiometricFlow` interface. The shipped `ClientBiometricFlow` class is the Neurotechnology reference implementation. Your job as an integrator is to provide an equivalent implementation for your vendor.

---

## Architecture overview

```
┌─────────────────────────────────────────┐
│           BRaVe Android App             │
│  (screens, workflows, business logic)   │
└──────────────────┬──────────────────────┘
                   │ uses only
                   ▼
┌─────────────────────────────────────────┐
│         BiometricFlow (interface)       │
│  init · createIntent · parseResult      │
│  createBundle · listIds · delete        │
│  enrollBatch                            │
└──────────────────┬──────────────────────┘
                   │ implemented by
         ┌─────────┴─────────┐
         ▼                   ▼
 ClientBiometricFlow    YourVendorBiometricFlow
   (Neurotechnology)      (your SDK)
```

**Key principle:** App code should depend on `BiometricFlow`, not on vendor SDK classes. Vendor-specific code lives in your implementation class (and any Activities or helpers it launches).

Reference files:

| File | Role |
|------|------|
| `BiometricFlow.java` | Contract the app expects |
| `ClientBiometricFlow.java` | Neurotechnology reference — use as a blueprint, do not modify for other vendors |

---

## Before you start

1. Obtain a licensed SDK from your biometric vendor (fingerprint / multimodal as required by the app).
2. Confirm the SDK supports the operations you need:
   - License / activation (if applicable)
   - Live capture (enroll)
   - Verification (1:1)
   - Identification (1:N) — if the app uses it
   - Template export as `byte[]` for batch enrollment
3. Add the vendor SDK to the project (JARs/AARs, native libs, scanner files) per that vendor’s documentation — analogous to the Neurotec layout described in `ANDROID_APP_README.md`, but paths and file names will differ.

---

## Step 1 — Create your implementation class

Create a new class that implements `BiometricFlow`, for example:

```java
package com.neurotec.core.multibiometric.brave;

import com.neurotec.core.multibiometric.brave.interfaces.BiometricFlow;

public class AcmeBiometricFlow implements BiometricFlow {
    // implement all interface methods
}
```

Keep vendor SDK imports confined to this class and any Activities or helpers you add. Do not spread vendor types across the app.

Use `ClientBiometricFlow` as a structural reference for **when** each method is called and **what** it should return, but replace all Neurotec API usage with your vendor’s APIs.

---

## Step 2 — Implement each `BiometricFlow` method

### `init(Context ctx)`

**Purpose:** One-time SDK initialization (license check, native libs, logging paths, etc.).

**Reference (`ClientBiometricFlow`):** Configures Neurotec licensing, `NCore`, and temp directory.

**Your implementation should:**

- Initialize the vendor SDK using the application context.
- Run only once (use a static flag or singleton, as in the reference).
- Log and handle failures without crashing the app.

```java
@Override
public synchronized void init(Context ctx) {
    if (inited) return;
    // vendorSdk.initialize(ctx.getApplicationContext(), ...);
    inited = true;
}
```

---

### `createIntent(Context ctx, Operation op, Bundle extras)`

**Purpose:** Return an `Intent` that launches the correct vendor UI or flow for the requested operation.

**Operations** (from `BiometricFlow.Operation`):

| Operation | Purpose | Reference maps to |
|-----------|---------|-------------------|
| `ACTIVATION` | License / SDK activation | `ActivationActivity` |
| `PREFERENCES` | Scanner / modality settings | `MultimodalPreferences` |
| `CAPTURE_BIOMETRIC` | Enroll, verify, or identify | `MultiModalActivity` |

**Your implementation should:**

1. `switch` on `op` and pick the appropriate Activity (or your own wrapper Activity).
2. For `CAPTURE_BIOMETRIC`, validate required extras — at minimum `EXTRA_INDIVIDUAL_ID` when enrollment/editing requires a subject id (see reference).
3. Pass through all `extras` with `intent.putExtras(extras)`.

You will typically need to **create new Activities** (or reuse vendor sample Activities) that:

- Perform capture with the vendor SDK.
- On completion, call `setResult(RESULT_OK, intent)` with the extras described below.
- On failure or cancel, return `RESULT_CANCELED` or `RESULT_OK` with `EXTRA_ERROR` set.

---

### `parseResult(int resultCode, Intent data)`

**Purpose:** Normalize vendor Activity results into a `CaptureResult` the app understands.

**Your implementation should:**

```java
@Override
public CaptureResult parseResult(int resultCode, Intent data) {
    if (resultCode != Activity.RESULT_OK || data == null) {
        return new CaptureResult(false, null, null, "cancelled");
    }
    Operation op = /* read EXTRA_OP if your Activities set it */;
    String err = data.getStringExtra(EXTRA_ERROR);
    Bundle payload = data.getExtras();
    boolean ok = (err == null);
    return new CaptureResult(ok, op, payload, err);
}
```

Match this behaviour so upstream app code does not need changes.

---

### `createBundle(String subject, BundleType bundle_type)`

**Purpose:** Build the `Bundle` of extras passed into `createIntent` for capture flows.

**Bundle types:**

| `BundleType` | Intended use | Extras to set (see `BiometricFlow` constants) |
|--------------|--------------|-----------------------------------------------|
| `NEW_CAPTURE` | Enroll new subject | `EXTRA_INDIVIDUAL_ID`, `EXTRA_IS_VERIFY_ONLY = false` |
| `EDIT_EXISTING` | Update existing subject | `EXTRA_INDIVIDUAL_ID`, `EXTRA_IS_VERIFY_ONLY = true`, `EXTRA_ALLOW_IDENTIFY_SUBJECT = false` |
| `VERIFICATION_ONLY` | Verify without enrollment | `EXTRA_IS_VERIFY_ONLY = true`, `EXTRA_ALLOW_DATA_UPDATE = false` |

Your capture Activities must **read these extras** and branch accordingly (enroll vs verify vs edit). Mirror the reference `createBundle` logic even if your UI differs.

---

### `listIds()` and `delete(String subjectId)`

**Purpose:** List enrolled subject IDs and delete a subject from the vendor’s local gallery / database.

**Reference:** Delegates to `Model.getInstance().getClient()`.

**Your implementation should:**

- Query the vendor SDK’s enrolled user store (or your app’s DB if templates are stored there).
- Return a `String[]` of subject ids.
- Implement `delete` to remove the subject and its templates from the same store.

Ensure IDs are consistent with `EXTRA_INDIVIDUAL_ID` used during capture.

---

### `enrollBatch(List<BiometricSubject> subjects)`

**Purpose:** Enroll multiple subjects programmatically from pre-built templates (no live capture UI).

**Reference:** Converts each `BiometricSubject` (`id` + `template` byte array) into Neurotec `NSubject` / `NFTemplate` and runs an enroll task.

**Your implementation should:**

1. For each `BiometricSubject`, load `getTemplate()` into the vendor’s template format.
2. Enroll under `getId()`.
3. Return an array of successfully enrolled ids (or `null` if the input list is empty — same as reference).

Template format is **vendor-specific**. Document internally how raw `byte[]` templates map to your SDK. If templates from Neurotec cannot be reused, batch import is only for templates produced by **your** vendor.

---

## Step 3 — Honour the Intent extras contract

The app and `BiometricFlow` share a set of string keys. Your Activities should **consume** inputs from these keys and **produce** outputs using the same keys where applicable.

### Input extras (app → your capture flow)

| Constant | Type | Meaning |
|----------|------|---------|
| `EXTRA_INDIVIDUAL_ID` | String | Subject identifier |
| `EXTRA_IS_VERIFY_ONLY` | boolean | Verification-only mode |
| `EXTRA_IS_REGISTRATION_ONLY` | boolean | Registration-only mode |
| `EXTRA_ALLOW_IDENTIFY_SUBJECT` | boolean | Allow 1:N identify |
| `EXTRA_ALLOW_DATA_UPDATE` | boolean | Allow updating stored biometrics |
| `EXTRA_DELETE_IDENTIFIED_SUBJECT` | boolean | Delete after identify (if used) |
| `EXTRA_IS_FINGER_BIOMETRIC` | boolean | Fingerprint modality flag |
| `EXTRA_RESULT_TYPE` | — | Result handling mode (`ResultType` enum) |
| `EXTRA_IS_AUTH` | boolean | Authentication context |

### Output extras (your capture flow → app)

| Constant | Type | Meaning |
|----------|------|---------|
| `EXTRA_OP` | String | `Operation` name that completed |
| `EXTRA_ERROR` | String | Error message; `null` means success |
| `EXTRA_MATCHING_ID` | String | Matched subject id (verify/identify) |
| `EXTRA_RAW_DATA` | — | Raw capture / template payload (key is `"finger"` in code) |

Set `EXTRA_ERROR` on failure so `parseResult` returns `success = false`. On success, leave `EXTRA_ERROR` unset and include any ids or raw data the app expects.

---

## Step 4 — Replace Neurotechnology wiring in the app

After your class is complete, point the app at it instead of `ClientBiometricFlow`.

### 4.1 Find the binding point

Search the codebase for:

```text
ClientBiometricFlow
new ClientBiometricFlow
BiometricFlow
getClient()
Model.getInstance()
```

The reference implementation uses `Model.getInstance().getClient()` for gallery operations. Locate where that client is created and swap in your implementation.

### 4.2 Recommended pattern — factory or config flag

Avoid hard-coding the implementation in many places. Use one factory:

```java
public final class BiometricFlowFactory {
    private BiometricFlowFactory() {}

    public static BiometricFlow create() {
        // return new ClientBiometricFlow();      // Neurotechnology
        return new AcmeBiometricFlow();           // your vendor
    }
}
```

Then use `BiometricFlowFactory.create()` everywhere the app obtains a `BiometricFlow`.

### 4.3 Dependencies and Gradle

- Remove or make optional Neurotec JARs under `app/libs/neurotec/` when not using Neurotechnology.
- Add your vendor’s libraries to `app/build.gradle` (or a dedicated `app-vendor-acme` module).
- Keep `BiometricFlow.java` in a small shared module with **no** vendor dependencies if possible, so the interface stays vendor-neutral.

### 4.4 UI and manifest

- Register your Activities in `AndroidManifest.xml`.
- Remove or disable Neurotec Activities (`MultiModalActivity`, `ActivationActivity`, etc.) if they are no longer used.
- Update preferences / settings screens if they reference Neurotec-only options.

---

## Step 5 — Implement vendor capture Activities

`ClientBiometricFlow` delegates UI to Neurotec Activities. For another vendor you must supply equivalent screens.

Minimum behaviour for the main capture Activity:

1. Read extras from the launching `Intent`.
2. Initialize the vendor SDK (if not already done in `init`).
3. Run enroll, verify, or identify per `BundleType` / boolean flags.
4. Finish with:

```java
Intent result = new Intent();
result.putExtra(BiometricFlow.EXTRA_OP, BiometricFlow.Operation.CAPTURE_BIOMETRIC.name());
// on success:
result.putExtra(BiometricFlow.EXTRA_MATCHING_ID, matchedId);  // if applicable
// on failure:
result.putExtra(BiometricFlow.EXTRA_ERROR, "description");
setResult(Activity.RESULT_OK, result);  // or RESULT_CANCELED for user cancel
finish();
```

Repeat for `ACTIVATION` and `PREFERENCES` operations if the app uses them.

---

## Step 6 — Test checklist

| Scenario | Expected |
|----------|----------|
| `init()` on cold start | No crash; SDK ready |
| `ACTIVATION` | License flow completes; `parseResult` success |
| `NEW_CAPTURE` | Subject enrolled; id appears in `listIds()` |
| `EDIT_EXISTING` | Template updated for existing id |
| `VERIFICATION_ONLY` | Match / no-match without enrolling |
| `delete(id)` | Id removed from `listIds()` |
| `enrollBatch(...)` | All valid templates enrolled |
| User cancel | `parseResult` → `success = false`, `error = "cancelled"` |
| SDK error | `EXTRA_ERROR` set; app shows error path |

---

## Step 7 — Documentation for your fork

When you ship a non-Neurotec build, document for downstream developers:

1. Which vendor SDK version is required.
2. Where to place libraries and scanner files (your layout may differ from `app/libs/neurotec/`).
3. License activation steps for that vendor.
4. Whether templates are compatible with Neurotec (usually **no**).
5. The class name that implements `BiometricFlow` in your build.

---

## Summary workflow

```
1. Obtain vendor SDK + license
2. Add SDK binaries to the project (Gradle + native libs)
3. Create YourVendorBiometricFlow implements BiometricFlow
4. Implement init, createIntent, parseResult, createBundle,
   listIds, delete, enrollBatch
5. Add Activities for ACTIVATION, PREFERENCES, CAPTURE_BIOMETRIC
6. Honour BiometricFlow EXTRA_* contract on Intent in/out
7. Wire app to YourVendorBiometricFlow (factory / Model / DI)
8. Remove or optionalize Neurotec dependencies
9. Run the test checklist above
```

---

## Related documents

- `ANDROID_APP_README.md` — Neurotechnology SDK setup (default vendor)
- `BiometricFlow.java` — interface contract
- `ClientBiometricFlow.java` — Neurotechnology reference implementation
