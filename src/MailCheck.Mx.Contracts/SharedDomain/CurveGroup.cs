namespace MailCheck.Mx.Contracts.SharedDomain
{
    public enum CurveGroup : ushort
    {
        /*
         * RFC 4492 5.1.1
         * 
         * The named curves defined here are those specified in SEC 2 [13]. Note that many of these curves
         * are also recommended in ANSI X9.62 [7] and FIPS 186-2 [11]. Values 0xFE00 through 0xFEFF are
         * reserved for private use. Values 0xFF01 and 0xFF02 indicate that the client supports arbitrary
         * prime and characteristic-2 curves, respectively (the curve parameters must be encoded explicitly
         * in ECParameters).
         */
        Sect163k1 = 0x0001,
        Sect163r1 = 0x0002,
        Sect163r2 = 0x0003,
        Sect193r1 = 0x0004,
        Sect193r2 = 0x0005,
        Sect233k1 = 0x0006,
        Sect233r1 = 0x0007,
        Sect239k1 = 0x0008,
        Sect283k1 = 0x0009,
        Sect283r1 = 0x000a,
        Sect409k1 = 0x000b,
        Sect409r1 = 0x000c,
        Sect571k1 = 0x000d,
        Sect571r1 = 0x000e,
        Secp160k1 = 0x000f,
        Secp160r1 = 0x0010,
        Secp160r2 = 0x0011,
        Secp192k1 = 0x0012,
        Secp192r1 = 0x0013,
        Secp224k1 = 0x0014,
        Secp224r1 = 0x0015,
        Secp256k1 = 0x0016,
        Secp256r1 = 0x0017,
        Secp384r1 = 0x0018,
        Secp521r1 = 0x0019,

        /*
         * RFC 7027
         */
        brainpoolP256r1 = 26,
        brainpoolP384r1 = 27,
        brainpoolP512r1 = 28,

        /*
         * RFC 8422
         */
        x25519 = 29,
        x448 = 30,

        /*
         * RFC 8734
         */
        brainpoolP256r1tls13 = 31,
        brainpoolP384r1tls13 = 32,
        brainpoolP512r1tls13 = 33,

        /*
         * draft-smyshlyaev-tls12-gost-suites-10
         */
        GC256A = 34,
        GC256B = 35,
        GC256C = 36,
        GC256D = 37,
        GC512A = 38,
        GC512B = 39,
        GC512C = 40,

        /*
         * RFC 8998
         */
        curveSM2 = 41,

        /*
         * RFC 7919 2. Codepoints in the "Supported Groups Registry" with a high byte of 0x01 (that is,
         * between 256 and 511, inclusive) are set aside for FFDHE groups, though only a small number of
         * them are initially defined and we do not expect many other FFDHE groups to be added to this
         * range. No codepoints outside of this range will be allocated to FFDHE groups.
         */
        Ffdhe2048 = 0x0100,
        Ffdhe3072 = 0x0101,
        Ffdhe4096 = 0x0102,
        Ffdhe6144 = 0x0103,
        Ffdhe8192 = 0x0104,

        /*
         * RFC 8446 reserved ffdhe_private_use (0x01FC..0x01FF)
         */

        /*
         * RFC 4492 reserved ecdhe_private_use (0xFE00..0xFEFF)
         */
        Unknown = 0xfe00,
        UnknownGroup1024 = 0xfe01,
        UnknownGroup1536 = 0xfe13,
        UnknownGroup2048 = 0xfe02,
        UnknownGroup3072 = 0xfe03,
        UnknownGroup4096 = 0xfe04,
        UnknownGroup6144 = 0xfe05,
        UnknownGroup8192 = 0xfe06,
        Java1024 = 0xfe10,
        Rfc2409_1024 = 0xfe11,
        Rfc5114_1024 = 0xfe12,

        /*
         * RFC 4492
         */
        arbitrary_explicit_prime_curves = 0xFF01,
        arbitrary_explicit_char2_curves = 0xFF02,
    }
}
