# EduCore — Data Protection Impact Assessment (draft)

**Status: technical draft for the controller to complete.** This describes what the system
actually does with personal data, taken from the code. The legal assessment, the lawful basis, the
retention periods and the sign-off belong to the controller (the ministry or school authority) and
its Data Protection Officer. Sections marked **[controller]** need their decision.

Reference law: Kosovo Law No. 06/L-082 on Protection of Personal Data — Art. 35 (impact
assessment before high-risk processing), Art. 33 (breach notification to the Information and
Privacy Agency within 72 hours), Art. 37 (DPO mandatory for public authorities), Art. 7 (children
under 16), Art. 86(3) (linking filing systems through a connecting code such as a national ID).

## 1. The processing

| | |
| --- | --- |
| Purpose | Running schools: enrolment, timetables, lessons, attendance, marks, assignments, records, guardian contact, and aggregate statistics for the education authorities |
| Controller | **[controller]** |
| Processor(s) | Hosting: Vercel (web app), Render (API), Neon (PostgreSQL), Resend (email), S3-compatible storage (documents) — **[controller: confirm DPAs and data locations]** |
| Data subjects | Pupils (mostly minors), guardians, teachers, school office staff, administrators |
| Scale | National: every school, pupil and guardian in the system |

## 2. Personal data held

| Category | Fields | Protection |
| --- | --- | --- |
| Identity | Names, email, date of birth, gender, address, origin country and city | School isolation (query filter, write interceptor, composite keys) |
| National identifiers | Pupil personal number, guardian national ID | AES-256 deterministic encryption with integrity check; key rotation |
| Health (special category) | Blood type, allergies, conditions, medications, emergency contact, notes | AES-256-GCM per field; read limited to office and homeroom teacher; every read audited |
| Education records | Marks (with change history and reasons), attendance, behaviour marks, lessons, assignments and submissions, schooling history, documents | School isolation; marks locked when a term closes; changes audited |
| Communication | Teacher–guardian messages, notifications | Visible to the two participants only |
| Credentials | Password hashes (PBKDF2-SHA512, 220k iterations), authenticator secrets (sealed), hashed sign-in codes and reset tokens | Never stored in plaintext |
| Activity | Audit log: who changed what, sign-ins, failed sign-ins, reads of health data and identifiers | Retained `Retention:AuditLogDays` (default 730) |

## 3. Necessity and proportionality

- **Lawful basis** — **[controller]**. For a state school system this is most likely a legal
  obligation or public task, not consent; Art. 7's parental consent applies to information-society
  services offered to children, which this is not.
- **Minimisation** — statistics are aggregate, with small cells (< 5) and their complements
  withheld so a hidden value cannot be recovered by subtraction.
- **Retention** — audit rows and spent tokens are purged automatically. How long a pupil's records
  are kept after they leave school is **[controller]**; nothing in the code deletes them.
- **Subject rights** — access, rectification and erasure are handled by the school office through
  the application; there is no self-service export yet. **[controller: decide whether one is needed]**

## 4. Risks and measures

| Risk | Measures in place | Residual / open |
| --- | --- | --- |
| One school's staff reading another school's pupils | Three layers of school isolation; integration-tested | Admin tiers cross schools by design; their actions are audited |
| Stolen session | 1-hour tokens in HttpOnly cookies; sign-out, password change and resets end every session; CSRF protection; security headers | — |
| Account takeover | 15-character passwords with a blocklist; per-account lockout; email change and 2FA removal need the password; authenticator-app second factor | Email codes remain the fallback second factor; passkeys not yet offered |
| Leak of the database | Identifiers and health data encrypted; keys outside the database; password hashes slow | Names, marks and attendance are not encrypted (needed for search and statistics) |
| Grade tampering | Closed terms lock marks; corrections need a reason; full change history | — |
| Undetected breach | Audit of sign-ins, failures, health and identifier reads; OpenTelemetry traces and logs | No alerting on the audit stream yet — **[controller: who watches it, and how 72-hour notification is met]** |
| Linking registries via national ID (Art. 86(3)) | IDs are stored encrypted and not exported | Any integration with another registry needs the Agency's prior authorisation |
| Third-party telemetry | None: no analytics or web-vitals collection is enabled | Enabling it would send page URLs (with record ids) to the provider — assess first |

## 5. Open items for sign-off

1. Lawful basis and legal retention periods for pupil records.
2. Data processing agreements and data locations for each processor in §1.
3. DPO appointment and contact (Art. 37).
4. Breach-response owner and procedure for the 72-hour notification (Art. 33).
5. Decision on making the authenticator app (or passkeys) mandatory for the administrative tiers.
6. Purge of `seed-dump.sql` from the public repository history and rotation of the shared seed
   password (see the security report).
