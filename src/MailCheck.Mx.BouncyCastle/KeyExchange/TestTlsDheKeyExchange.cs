using System.Collections;
using System.IO;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Tls;
using Org.BouncyCastle.Utilities.IO;

namespace MailCheck.Mx.BouncyCastle.KeyExchange
{
    internal class TestTlsDheKeyExchange : TlsDheKeyExchange
    {
        private SignatureAndHashAlgorithm mSignatureAndHashAlgorithm;

        public TestTlsDheKeyExchange(int keyExchange, IList supportedSignatureAlgorithms, TlsDHVerifier tlsDHVerifier, DHParameters dhParameters)
            : base(keyExchange, supportedSignatureAlgorithms, tlsDHVerifier, dhParameters)
        {
        }

        public DHParameters DhParameters => mDHParameters;

        public SignatureAndHashAlgorithm EcSignatureAndHashAlgorithm => mSignatureAndHashAlgorithm;

        public override void ProcessServerKeyExchange(Stream input)
        {
            SecurityParameters securityParameters = mContext.SecurityParameters;

            SignerInputBuffer buf = new SignerInputBuffer();
            Stream teeIn = new TeeInputStream(input, buf);

            this.mDHParameters = TlsDHUtilities.ReceiveDHParameters(mDHVerifier, teeIn);
            this.mDHAgreePublicKey = new DHPublicKeyParameters(TlsDHUtilities.ReadDHParameter(teeIn), mDHParameters);

            DigitallySigned signed_params = ParseSignature(input);
            mSignatureAndHashAlgorithm = signed_params.Algorithm;

            ISigner signer = InitVerifyer(mTlsSigner, signed_params.Algorithm, securityParameters);
            buf.UpdateSigner(signer);
            if (!signer.VerifySignature(signed_params.Signature))
                throw new TlsFatalAlert(AlertDescription.decrypt_error);
        }
    }
}