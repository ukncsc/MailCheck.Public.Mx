namespace MailCheck.Mx.Contracts.SharedDomain
{
    public enum SignatureHashAlgorithm : ushort
    {
        // Copied from...
        // https://github.com/bcgit/bc-csharp/blob/2a252b81561813ea709080753ba9c125c87397a4/crypto/src/tls/SignatureScheme.cs

        SHA512_RSA = 0x0601,
        SHA512_DSA = 0x0602,
        SHA512_ECDSA = 0x0603,
        SHA384_RSA = 0x0501,
        SHA384_DSA = 0x0502,
        SHA384_ECDSA = 0x0503,
        SHA256_RSA = 0x0401,
        SHA256_DSA = 0x0402,
        SHA256_ECDSA = 0x0403,
        SHA224_RSA = 0x0301,
        SHA224_DSA = 0x0302,
        SHA224_ECDSA = 0x0303,
        SHA1_RSA = 0x0201,
        SHA1_DSA = 0x0202,
        SHA1_ECDSA = 0x0203,

        UNKNOWN = 0xffff,

        /*
         * RFC 8446
         */

        rsa_pkcs1_sha1 = 0x0201,
        ecdsa_sha1 = 0x0203,

        rsa_pkcs1_sha256 = 0x0401,
        rsa_pkcs1_sha384 = 0x0501,
        rsa_pkcs1_sha512 = 0x0601,

        ecdsa_secp256r1_sha256 = 0x0403,
        ecdsa_secp384r1_sha384 = 0x0503,
        ecdsa_secp521r1_sha512 = 0x0603,

        rsa_pss_rsae_sha256 = 0x0804,
        rsa_pss_rsae_sha384 = 0x0805,
        rsa_pss_rsae_sha512 = 0x0806,

        ed25519 = 0x0807,
        ed448 = 0x0808,

        rsa_pss_pss_sha256 = 0x0809,
        rsa_pss_pss_sha384 = 0x080A,
        rsa_pss_pss_sha512 = 0x080B,

        /*
         * RFC 8734
         */

        ecdsa_brainpoolP256r1tls13_sha256 = 0x081A,
        ecdsa_brainpoolP384r1tls13_sha384 = 0x081B,
        ecdsa_brainpoolP512r1tls13_sha512 = 0x081C,

        /*
         * RFC 8998
         */

        sm2sig_sm3 = 0x0708,

        /*
         * RFC 8446 reserved for private use (0xFE00..0xFFFF)
         */
    }
}
