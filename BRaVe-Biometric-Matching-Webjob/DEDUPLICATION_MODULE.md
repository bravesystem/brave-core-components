# Biometric Deduplication Service

This document describes the **deduplication service** in the BRaVe project: what it does, how it is deployed, required configuration, and how to replace it when using a biometric vendor other than Neurotechnology.

---

## Deployment

In **BRaVe**, the deduplication module has been **containerized** (Docker) and is the recommended way to run it in production and most dev environments.

The same application can also run as a **.NET console app** without containers, for example:

| Deployment | When to use |
|------------|-------------|
| **Container** (default in BRaVe) | Kubernetes, Azure Container Apps, Docker on a VM, CI/CD pipelines |
| **Console on a virtual machine** | Simple VM deployment, legacy hosts, debugging |
| **Console / container on a cloud worker** | Any cloud provider that runs containers or long-lived console/worker processes |

The hosting model is flexible; the **runtime behaviour and environment variables are the same** regardless of whether you use a container, a VM, or a managed worker service.

```
Message queue (jobId)
        │
        ▼
┌───────────────────────────┐
│  Deduplication service    │  ← container, VM console, or cloud worker
└───────────┬───────────────┘
            │ fetch biometrics for jobId
            ▼
┌───────────────────────────┐
│  Process & match          │──────► Matching server
└───────────┬───────────────┘
            │
     match? ├── Yes ──► do not store (duplicate)
            │
            └── No  ──► store on Matching server for future checks
```

---

## Purpose

Before new biometrics are persisted on the **Matching server**, the system checks whether they already exist in the gallery (duplicate / same person). The deduplication service:

1. Consumes jobs from a **message queue**.
2. Uses the **job ID** in each message to load all biometric data attached to that job.
3. Runs **identification / matching** against the Matching server.
4. **Stores** biometrics only when **no match** is found — so future jobs can be compared against them.
5. **Skips storage** when a **match** is found — avoiding duplicate enrollments on the Matching server.

---

## High-level flow

| Step | Action |
|------|--------|
| 1 | Service reads a message from the queue containing a **job ID**. |
| 2 | Using that job ID, it **fetches all biometric records** linked to the job (templates, modalities, metadata as defined by your data layer). |
| 3 | For each biometric (or batch, depending on implementation), it invokes the **Matching server** to search the existing gallery. |
| 4 | **Match found** → biometric is treated as a duplicate; it is **not** enrolled/stored on the Matching server. |
| 5 | **No match** → biometric is **stored** on the Matching server so later jobs can be deduplicated against it. |
| 6 | Job processing completes (success/failure handling and queue ack/nack per your messaging setup). |

Exact queue technology, APIs, and error/retry policy depend on your deployment; this module’s responsibility is **match-then-store-or-skip** logic tied to a **job ID**.

---

## Default implementation: Neurotechnology

The shipped deduplication module is built for the **Neurotechnology Biometric SDK** and targets **.NET Framework 4.7**.

### Why .NET Framework 4.7?

The Neurotechnology SDK and matching components used in this module are constrained by **what the vendor supports** for server-side / matching integration. At the time of this release, that limits the console app to **.NET Framework 4.7** rather than .NET 6+.

Implications for contributors:

- Build and run this project with the **.NET Framework 4.7** developer pack / MSBuild toolchain.
- Reference Neurotec matching libraries per your internal or documented SDK layout (see [BIOMETRIC_VENDOR_INTEGRATION.md](./BIOMETRIC_VENDOR_INTEGRATION.md) and [ANDROID_APP_README.md](./ANDROID_APP_README.md) for related Neurotec binary setup where applicable).
- A valid **Neurotechnology license** is required for matching operations in your environment.

---

## Neurotechnology Matching Server

The deduplication service depends on a **Neurotechnology Matching server** running separately from the deduplication container or console app. In BRaVe, the Matching server is provided as a **container image** that you install on **Linux Ubuntu** — either a virtual machine or a physical machine.

### Installation

1. Provision an **Ubuntu** host (VM or bare metal) that meets Neurotechnology’s requirements for the Matching server.
2. Obtain the **installation package** (ZIP with container images and supporting files) — see [Obtaining the Matching server package](#obtaining-the-matching-server-package) below.
3. Follow the **installation guide (PDF)** included with the package (or linked below) for step-by-step setup, networking, ports, and license activation.
4. After installation, set `MATCHING_SERVER_HOSTNAME` and `MATCHING_SERVER_PARAMS` on the deduplication service so it can reach the Matching server (admin port `AP`, client port `CP`, etc.).

**Installation guide (PDF):** [MMA Cluster Manual — Neurotechnology Matching Server installation guide](./docs/MMA_Cluster_Manual.pdf)

### Obtaining the Matching server package

| Option | Who it is for |
|--------|----------------|
| **Download ZIP** | Teams with a Neurotechnology license and infrastructure to host the Matching server |
| **Contact IOM** | Contributors who need access without procuring their own license or hardware |

**Matching server package (ZIP):**  
[Download matching server installation package](https://iompwesabrave002.blob.core.windows.net/build-artifacts/matching-server.zip)  
*(Replace or supplement this URL when your final distribution link is available.)*

The ZIP contains the files required to deploy the Neurotechnology Matching server container on Ubuntu.

### Contributors without a local Matching server

If you want to **contribute** to BRaVe but do not have a Neurotechnology license, suitable hardware, or capacity to host your own Matching server, **contact IOM** to request access to an **existing development Matching server** used for contribution and integration testing.

- Open a [GitHub Issue](https://github.com/bravesystem/brave-core-components/issues) describing your contribution plan and that you need shared dev Matching server access, **or**
- Use the contact path described in [CONTRIBUTING.md](./CONTRIBUTING.md) and [SECURITY.md](./SECURITY.md).

IOM can provide hostname and connection parameters for development only (`MATCHING_SERVER_HOSTNAME`, `MATCHING_SERVER_PARAMS`). Do not use production credentials or production galleries for open-source development.

---

## Using another biometric vendor

The deduplication console is **not vendor-neutral** in the default codebase. Neurotechnology-specific APIs handle template format, matching, and gallery enrollment.

If you integrate a **different biometric vendor**, you should:

1. **Implement your own deduplication module** (new console app or pluggable library) that preserves the **same business behaviour**:
   - consume queue messages with a **job ID**;
   - load biometrics for that job;
   - match against your vendor’s **Matching server** (or equivalent 1:N / identify API);
   - **store only on non-match**; **skip storage on match**.

2. **Do not assume** template compatibility with Neurotechnology — templates from another SDK cannot be used with the default Matching server without conversion (usually not available).

3. **Replace or bypass** the Neurotechnology-based console project in your solution and wire your module to the same queue and job/biometric data APIs your deployment uses.

4. Document for your fork:
   - vendor SDK version and .NET version required;
   - matching server endpoint and authentication;
   - template format and enrollment APIs;
   - how job ID → biometric payload retrieval works in your stack.

See [BIOMETRIC_VENDOR_INTEGRATION.md](./BIOMETRIC_VENDOR_INTEGRATION.md) for the parallel pattern on the Android side (`BiometricFlow` / `ClientBiometricFlow`). The deduplication service is the **server-side counterpart**: there is no shared interface in-repo today; a new implementation should mirror the **workflow** above, not necessarily the Neurotec class names.

---

## Suggested contract for a custom module

When implementing an alternate vendor module, keep these behavioural guarantees aligned with the rest of BRaVe:

| Contract | Requirement |
|----------|-------------|
| Input | Queue message with **job ID** (and any existing metadata your pipeline expects) |
| Data load | Resolve **all biometrics** attached to the job before matching |
| Match | Query Matching server / identify API with vendor templates |
| On match | **Do not** store biometric on Matching server; record outcome for the job as your app requires |
| On no match | **Store** biometric on Matching server for future deduplication |
| Idempotency | Define whether reprocessing the same job ID is safe (recommended: yes, or use job status flags) |
| Failure | Failed match/store should not ack the message (or should dead-letter) per your messaging policy |

---

## Required environment variables

Regardless of deployment (container, VM console, or cloud worker), the following variables **must** be set for the service to work. Do **not** commit real values to git — use environment configuration, Azure Key Vault, or your platform’s secret store.

| Variable | Description | Example |
|----------|-------------|---------|
| `MATCHING_SERVER_HOSTNAME` | Matching server hostname or IP address | `10.0.1.50` or `matching.internal.example` |
| `MATCHING_SERVER_PARAMS` | Matching server admin and client port access, plus matching thresholds | `AP:24932,CP:25452,MINSCORE:200,THRESH:70` |
| `AZURE_SB_CONNECTIONSTRING` | Azure Service Bus connection string | `Endpoint=sb://...` (from Azure Portal) |
| `AZURE_SB_QUEUE_NAME` | Message queue the service listens on | `biometric-matching-requests` |
| `AZURE_SQL_CONNECTIONSTRING` | SQL Server connection string used to resolve job ID → biometric data | `Server=tcp:...;Database=...;...` |

### Parameter reference

**`MATCHING_SERVER_PARAMS`** — comma-separated key:value pairs:

| Key | Meaning |
|-----|---------|
| `AP` | Admin port |
| `CP` | Client port |
| `MINSCORE` | Minimum match score |
| `THRESH` | Match threshold |

Adjust values to match your Matching server deployment and matching policy.

### Example (local / Docker — placeholders only)

```bash
MATCHING_SERVER_HOSTNAME=matching.server.ip.address
MATCHING_SERVER_PARAMS=AP:24932,CP:25452,MINSCORE:200,THRESH:70
AZURE_SB_CONNECTIONSTRING=Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...
AZURE_SB_QUEUE_NAME=biometric-matching-requests
AZURE_SQL_CONNECTIONSTRING=Server=tcp:your-server.database.windows.net,1433;Database=YourDb;User ID=...;Password=...;Encrypt=True;
```

For Docker, pass these via `docker run -e ...`, `docker-compose.yml` `environment:`, or Kubernetes secrets / ConfigMaps. For a VM console app, set them in the shell, Windows service environment, or systemd unit.

Vendor SDK license / activation settings may require additional variables depending on your Neurotechnology deployment — configure those separately and keep them out of source control.

---

## Related documents

| Document | Topic |
|----------|--------|
| [BIOMETRIC_VENDOR_INTEGRATION.md](./BIOMETRIC_VENDOR_INTEGRATION.md) | Android `BiometricFlow` and alternate vendors |
| [ANDROID_APP_README.md](./ANDROID_APP_README.md) | Neurotec libraries and scanners for the mobile app |
| [CONTRIBUTING.md](./CONTRIBUTING.md) | How to contribute; Azure DevOps as source of truth |
| [MMA Cluster Manual (PDF)](./docs/MMA_Cluster_Manual.pdf) | Neurotechnology Matching Server installation |

---

## Summary

| Topic | Detail |
|-------|--------|
| **Role** | Queue-driven deduplication before Matching server enrollment |
| **Trigger** | Message with **job ID** → fetch job biometrics → match → store or skip |
| **Deployment** | **Containerized** in BRaVe; also runnable as console on VM or cloud worker |
| **Configuration** | `MATCHING_SERVER_*`, `AZURE_SB_*`, `AZURE_SQL_CONNECTIONSTRING` (required) |
| **Matching server** | Neurotechnology container on **Ubuntu** (VM or physical); PDF install guide; ZIP or contact IOM |
| **Contributors** | Shared **dev** Matching server available via IOM if you lack license/hardware |
| **Default stack** | Neurotechnology SDK, **.NET Framework 4.7** |
| **Other vendors** | Implement a **replacement deduplication module** with the same match / no-store vs no-match / store behaviour |
