using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace RethinkDb.Driver.Net
{
    /// <summary>
    /// Settings object for SSL/TLS connections with RethinkDB.
    /// </summary>
    public class SslContext
    {
        /// <summary>
        /// Client certificates.
        /// </summary>
        public X509CertificateCollection ClientCertificateCollection { get; set; } = new X509Certificate2Collection();

        /// <summary>
        /// Server-side certificate validation callback.
        /// </summary>
        public RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
        /// <summary>
        /// Client-side certificate validation callback
        /// </summary>
        public LocalCertificateSelectionCallback LocalCertificateSelectionCallback { get; set; }

        /// <summary>
        /// The enabled security protocols to use over the socket. Default: TLS, TLS 1.1, TLS 1.2.
        /// SSLv2 and SSLv3 are considered insecure.
        /// </summary>
        public SslProtocols EnabledProtocols { get; set; } = 
            SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;

        /// <summary>
        /// By default, the connection's hostname is used. This setting can override host verification.
        /// </summary>
        public string TargetHostOverride { get; set; }

        /// <summary>
        /// Check for certificate revocation.
        /// </summary>
        public bool CheckCertificateRevocation { get; set; } = false;
    }
}