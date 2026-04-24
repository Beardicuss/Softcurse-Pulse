# Security Policy

## Supported Versions

Only the most recent major iteration of Pulse receives ongoing critical security updates. If you are experiencing vulnerabilities on older branches, please upgrade to the stable V3 baseline before submitting a report.

| Version | Supported          |
| ------- | ------------------ |
| 3.x.x   | ✅                  |
| 2.x.x   | ❌                  |
| 1.x.x   | ❌                  |

## Reporting a Vulnerability

**Do NOT open a public GitHub Issue for a security vulnerability.**

If you discover a vulnerability in the `Pulse.Core` codebase or a loophole within our SQLite execution pipelines, please report it via **GitHub Private Vulnerability Reporting** located within the "Security" tab of this repository.

When reporting, please include:
- A clear description of the vulnerability.
- Step-by-step reproduction instructions.
- The perceived impact on end-user machines (e.g. Remote Code Execution, Privilege Escalation).
- Proof-of-concept shell scripts, screenshots, or memory dumps.

## Response Timeline

We treat all security reports with the highest priority:
- **Acknowledgement**: Within 48 hours of submission.
- **Initial assessment**: Within 7 days.
- **Patch or mitigation deployment**: Within 90 days of confirmation.
- **Public disclosure**: Coordinated and released via GitHub Security Advisories immediately following the distribution of patches to our user base.

## Out of Scope

The following items are NOT considered vulnerabilities and will be dismissed immediately if filed as such:
- Social engineering attacks against maintainers or contributors.
- Denial of Service (DoS) attacks on webhook endpoints (Telegram/Discord servers are beyond our purview).
- Vulnerabilities relying strictly upon the user voluntarily disabling UAC / Windows Firewall constraints manually outside of Pulse.
- Issues targeting deprecated application versions.

## Bug Bounty

Currently, the Pulse System Monitor is a deeply open-source endeavor and we **do not** offer financial bug bounties. However, researchers submitting validated vulnerabilities will be formally recognized in our Security Advisories and Hall of Fame!

## Security Best Practices for Contributors

If you are developing a local Plugin for Pulse or modifying core logic:
- **Never commit `appsettings.json`** to public branches. If you notice a branch containing webhooks, alert maintainers for immediate cache scrubbing.
- Rely natively on `.NET 8` bounds checking and strictly validate arguments passed into the `netsh` Firewall blocks.
- Explicitly avoid storing user data beyond internal system metrics inside `pulse.db`.
