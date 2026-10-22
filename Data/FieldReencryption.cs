using EduCore.Security.Encryption;
using Microsoft.EntityFrameworkCore;

namespace EduCore.Data;

// Rewrites encrypted columns under the current key: values written under a rotated-out key, and
// legacy plaintext from before a column was encrypted. Runs at start-up after migrations (Render
// offers nowhere else to run it), and is idempotent -- once everything is current it reads only
// the ids of rows that still need it, which is none.
//
// Plain SQL rather than tracked entities on purpose: the school-scope interceptor rightly refuses
// writes from a caller with no school, and this runs with no caller at all. It touches nothing but
// the encrypted column it is rewriting.
public static class FieldReencryption
{
    // Deterministic columns must stay deterministic -- they back unique indexes and lookups.
    private static readonly (string Table, string Column, bool Searchable)[] Columns =
    [
        ("Students", "PersonalNumber", true),
        ("Guardians", "NationalId", true),
        ("StudentMedicalRecords", "BloodType", false),
        ("StudentMedicalRecords", "Allergies", false),
        ("StudentMedicalRecords", "ChronicConditions", false),
        ("StudentMedicalRecords", "Medications", false),
        ("StudentMedicalRecords", "EmergencyContactName", false),
        ("StudentMedicalRecords", "EmergencyContactPhone", false),
        ("StudentMedicalRecords", "Notes", false),
        ("Users", "TotpSecret", false),
    ];

    private sealed record Row(Guid Id, string Value);

    public static async Task<int> RunAsync(AppDbContext db, IFieldEncryptor encryptor, CancellationToken ct)
    {
        var rewritten = 0;

        foreach (var (table, column, searchable) in Columns)
        {
            // Table and column names come from the constant list above, never from input.
#pragma warning disable EF1002
            var rows = await db.Database
                .SqlQueryRaw<Row>($"""SELECT "Id", "{column}" AS "Value" FROM "{table}" WHERE "{column}" IS NOT NULL AND "{column}" <> ''""")
                .ToListAsync(ct);

            foreach (var row in rows.Where(r => encryptor.NeedsReencryption(r.Value)))
            {
                var plaintext = encryptor.Decrypt(row.Value);
                var value = searchable ? encryptor.Encrypt(plaintext) : encryptor.Seal(plaintext);

                rewritten += await db.Database.ExecuteSqlRawAsync(
                    $$"""UPDATE "{{table}}" SET "{{column}}" = {0} WHERE "Id" = {1}""", [value, row.Id], ct);
            }
#pragma warning restore EF1002
        }

        return rewritten;
    }
}
