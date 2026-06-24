# Contributing to brave-core-components

Thank you for your interest in contributing. This project is open source on GitHub; internal development is maintained in Azure DevOps. We welcome bug reports, documentation improvements, and code contributions from the community.

---

## How to contribute

### 1. Open an issue (recommended)

For significant changes, open an [issue](https://github.com/bravesystem/brave-core-components/issues) first to discuss:

- What problem you are solving
- Your proposed approach
- Whether the change fits the project scope

Small fixes (typos, clear bugs) can go straight to a pull request.

### 2. Fork and branch

1. [Fork](https://github.com/bravesystem/brave-core-components/fork) this repository.
2. Clone your fork locally.
3. Create a branch from `main`:

```bash
git checkout main
git pull upstream main
git checkout -b feature/your-short-description
```

### 3. Make your changes

- Follow existing code style and project structure.
- Do **not** commit secrets, credentials, license keys, or proprietary SDK binaries (e.g. Neurotechnology SDK files).
- Update documentation when behaviour or setup steps change.
- Keep pull requests focused — one logical change per PR when possible.

### 4. Open a pull request

1. Push your branch to your fork.
2. Open a **Pull Request** against `bravesystem/brave-core-components` → `main`.
3. Fill in the PR description: what changed, why, and how to test.
4. Link any related issue (e.g. `Fixes #123`).

A maintainer will review your PR. **At least one approval is required** before changes are accepted into `main`.

---

## Pull request guidelines

| Do | Don't |
|----|-------|
| Keep changes small and reviewable | Mix unrelated refactors with feature work |
| Add or update docs for user-facing changes | Commit API keys, passwords, or `.env` files |
| Test your changes locally when possible | Include proprietary third-party SDKs |
| Respond to review feedback | Force-push without notice after review has started (re-request review instead) |

---

## How contributions are merged

GitHub is the **public** repository. **Azure DevOps is the source of truth** for ongoing development.

Typical flow for accepted contributions:

1. You open a PR on GitHub.
2. Maintainers review and approve on GitHub.
3. Approved changes are integrated into Azure DevOps (often via cherry-pick).
4. The public GitHub `main` branch is updated from Azure DevOps.

Because of this mirror workflow, your PR may show as merged or closed after maintainers port the change internally. The important part is that **approved work lands in the project** — not necessarily via the GitHub “Merge” button alone.

If you need clarity on status, comment on your PR or issue.

---

## Reporting security issues

**Do not** open a public issue for security vulnerabilities.

Report security concerns privately as described in [SECURITY.md](./SECURITY.md) (or contact the maintainers listed there).

---

## Development setup

- **Android app (Neurotechnology default):** see [ANDROID_APP_README.md](./ANDROID_APP_README.md) for SDK layout, build, and run instructions.
- **Alternative biometric vendor:** see [BIOMETRIC_VENDOR_INTEGRATION.md](./BIOMETRIC_VENDOR_INTEGRATION.md).
- **Deduplication service:** see [DEDUPLICATION_MODULE.md](./DEDUPLICATION_MODULE.md).
- **Matching server install:** [MMA Cluster Manual (PDF)](./docs/MMA_Cluster_Manual.pdf)

Supplementary files may be available on the [Releases](https://github.com/bravesystem/brave-core-components/releases) page.

### Shared development resources

If you lack a Neurotechnology license or infrastructure to host a Matching server, open a [GitHub Issue](https://github.com/bravesystem/brave-core-components/issues) to request access to IOM’s shared **development** Matching server for contribution purposes.

---

## Code of conduct

Be respectful and constructive in issues, pull requests, and discussions. Harassment or abusive behaviour is not tolerated.

---

## Questions

- **Bugs and features:** [GitHub Issues](https://github.com/bravesystem/brave-core-components/issues)
- **Usage and setup:** check the README and linked guides above

We appreciate your contributions.
