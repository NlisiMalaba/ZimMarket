namespace ZimMarket.Infrastructure.Configuration;

public sealed class FirebaseAdminOptions
{
    public const string SectionName = "Firebase";

    /// <summary>Optional explicit project ID (otherwise inferred from service account JSON).</summary>
    public string? ProjectId { get; set; }

    /// <summary>Full service account JSON (e.g. from Key Vault or environment variable).</summary>
    public string? CredentialsJson { get; set; }

    /// <summary>Path to the service account JSON key file.</summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// When true, uses <c>GoogleCredential.GetApplicationDefault()</c> (GCP metadata server,
    /// <c>GOOGLE_APPLICATION_CREDENTIALS</c>, etc.).
    /// </summary>
    public bool UseApplicationDefaultCredentials { get; set; }
}
