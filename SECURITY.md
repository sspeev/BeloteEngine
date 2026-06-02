# 🛡️ Security Policy

We take the security of the **Belote Engine API & Game Server** seriously. Thank you for helping us keep our backend engine safe, fair, and reliable for all players.

This document outlines our policy for reporting security vulnerabilities, our supported versions, and the practices we follow to ensure the integrity of the game.

---

## 📋 Supported Versions & Versioning Policy

We follow a structured Semantic Versioning (SemVer) model:
* **`x.0.0` (Major):** New major versions or substantial architectural changes/rewrites.
* **`0.x.0` (Minor):** New features or functional additions.
* **`0.0.x` (Patch):** Hotfixes, security patches, and general bug fixes.

We actively maintain and provide security updates for the following versions of the Belote Engine API:

| Version | Supported | Notes |
|---------|-----------|-------|
| `v1.0.x` |  Yes | Active stable release branch. |
| `< v1.0.0` |  No | Pre-release and development versions. Please upgrade to the latest release. |

---

## 🔒 Reporting a Vulnerability

**Please do not open a public GitHub issue for security vulnerabilities.**

If you discover a security vulnerability, we ask that you report it to us privately. This allows us to investigate, develop a fix, and coordinate a release before the issue is publicly disclosed.

### How to Report

To report a vulnerability, please email us at [peevstoyan05@gmail.com](mailto:[EMAIL_ADDRESS]) with the following details:

1. **Description:** A detailed description of the vulnerability and its potential impact.
2. **Steps to Reproduce:** A step-by-step guide (or proof-of-concept exploit) to reproduce the issue.
3. **Environment Details:** Runtime environment, hosting provider, or local development details.
4. **Attribution:** Let us know if you would like to be credited publicly in our release notes once the vulnerability is resolved.

We will acknowledge receipt of your report within **48 hours** and provide a status update on our investigation.

---

## 🛡️ Security Architecture & Best Practices

The Belote Engine backend implements several defense-in-depth measures to protect game state and API services:

### 1. Robust Session Validation
* **Data-Protected Session Cookies:** Player identity is bound to a cryptographically protected session cookie (`belote_session`) generated using ASP.NET Core's Data Protection APIs (`IDataProtector`).
* **Identity Verification:** On every critical hub method (e.g., `JoinLobby`, `MakeBid`, `PlayCard`), the backend validates the session payload and verifies that the calling connection's identity matches the player claiming the action, preventing session spoofing.

### 2. Network & Application Defense
* **Rate Limiting:** Fixed-window rate limiting is enabled across both controllers and hubs (`[EnableRateLimiting("fixed")]`) to protect endpoints against brute force and Distributed Denial of Service (DDoS) attempts.
* **IP Connection Limiting:** SignalR connections are limited per IP address (`IConnectionLimiter`) to prevent single clients from hogging server sockets.
* **Input Sanitization:** All incoming user text (player names, lobby names) is rigorously validated and sanitized (`InputValidator.SanitizePlayerName`) before database storage or memory caching to eliminate SQL Injection, Cross-Site Scripting (XSS), and directory traversal vectors.

### 3. Server-Driven Game State
* **Anti-Cheat Enforcement:** The backend acts as the single source of truth. Hands are dealt privately, and card details are only dispatched to the respective player's client. Game progression (turn validation, bid checking, and legal card play verification) is calculated and enforced on the server.

---

## 🚀 Disclosure Process

Once a vulnerability is reported, we follow a responsible disclosure process:

1. **Triage:** We investigate and confirm the vulnerability.
2. **Mitigation:** We develop a patch or workaround.
3. **Coordination:** We coordinate the release of the fix across the deployed frontend and backend services.
4. **Disclosure:** Once the patch is deployed and verified, we publish a security advisory detailing the vulnerability and thanking the reporter (if desired).
