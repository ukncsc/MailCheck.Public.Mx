using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using MailCheck.Mx.Contracts.SharedDomain;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsTester.Domain;
using MailCheck.Mx.TlsTester.Util;

namespace MailCheck.Mx.TlsTester.MxTester
{
    public interface ITlsSecurityTesterAdapator
    {
        Task<TlsTestResults> Test(TlsTestPending tlsTest);
        Task<TlsTestResults> Test(TlsTestPending tlsTest, int[] testIds);
    }

    public class TlsSecurityTesterAdapator : ITlsSecurityTesterAdapator
    {
        private readonly ITlsSecurityTester _tlsSecurityTester;

        public TlsSecurityTesterAdapator(ITlsSecurityTester tlsSecurityTester)
        {
            _tlsSecurityTester = tlsSecurityTester;
        }

        public Task<TlsTestResults> Test(TlsTestPending tlsTest)
        {
            return Test(tlsTest, null);
        }

        public async Task<TlsTestResults> Test(TlsTestPending tlsTest, int[] testIds)
        {
            List<TlsTestResult> results = new List<TlsTestResult>();

            List<X509Certificate2> certificates = new List<X509Certificate2>();

            if (!string.IsNullOrWhiteSpace(tlsTest.Id) && tlsTest.Id.Trim() != ".")
            {
                results = await _tlsSecurityTester.Test(tlsTest.Id, testIds);

                certificates = results.FirstOrDefault(_ => _.Result.Certificates.Any())?
                                   .Result.Certificates.ToList() ?? new List<X509Certificate2>();
            }

            BouncyCastleTlsTestResult tls12AvailableWithBestCipherSuiteSelected = ToTestResult(
                results.FirstOrDefault(_ => _.Test.Id == (int) TlsTestType.Tls12AvailableWithBestCipherSuiteSelected));

            BouncyCastleTlsTestResult tls12AvailableWithBestCipherSuiteSelectedFromReverseList = ToTestResult(
                results.FirstOrDefault(_ =>
                    _.Test.Id ==
                    (int) TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList));

            BouncyCastleTlsTestResult tls12AvailableWithSha2HashFunctionSelected = ToTestResult(results.FirstOrDefault(
                _ =>
                    _.Test.Id == (int) TlsTestType.Tls12AvailableWithSha2HashFunctionSelected));

            BouncyCastleTlsTestResult tls12AvailableWithWeakCipherSuiteNotSelected = ToTestResult(
                results.FirstOrDefault(_ =>
                    _.Test.Id == (int) TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected));

            BouncyCastleTlsTestResult tls11AvailableWithBestCipherSuiteSelected = ToTestResult(results.FirstOrDefault(
                _ =>
                    _.Test.Id == (int) TlsTestType.Tls11AvailableWithBestCipherSuiteSelected));

            BouncyCastleTlsTestResult tls11AvailableWithWeakCipherSuiteNotSelected = ToTestResult(
                results.FirstOrDefault(_ =>
                    _.Test.Id == (int) TlsTestType.Tls11AvailableWithWeakCipherSuiteNotSelected));

            BouncyCastleTlsTestResult tls10AvailableWithBestCipherSuiteSelected = ToTestResult(results.FirstOrDefault(
                _ =>
                    _.Test.Id == (int) TlsTestType.Tls10AvailableWithBestCipherSuiteSelected));

            BouncyCastleTlsTestResult tls10AvailableWithWeakCipherSuiteNotSelected = ToTestResult(
                results.FirstOrDefault(_ =>
                    _.Test.Id == (int) TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected));

            BouncyCastleTlsTestResult ssl3FailsWithBadCipherSuite = ToTestResult(results.FirstOrDefault(_ =>
                _.Test.Id == (int) TlsTestType.Ssl3FailsWithBadCipherSuite));

            BouncyCastleTlsTestResult tlsSecureEllipticCurveSelected = ToTestResult(results.FirstOrDefault(_ =>
                _.Test.Id == (int) TlsTestType.TlsSecureEllipticCurveSelected));

            BouncyCastleTlsTestResult tlsSecureDiffieHellmanGroupSelected = ToTestResult(results.FirstOrDefault(_ =>
                _.Test.Id == (int) TlsTestType.TlsSecureDiffieHellmanGroupSelected));

            BouncyCastleTlsTestResult tlsWeakCipherSuitesRejected = ToTestResult(results.FirstOrDefault(_ =>
                _.Test.Id == (int) TlsTestType.TlsWeakCipherSuitesRejected));


            return
                new TlsTestResults(tlsTest.Id,
                    IsErrored(results), 
                    CheckHostNotFound(results),
                    tls12AvailableWithBestCipherSuiteSelected,
                    tls12AvailableWithBestCipherSuiteSelectedFromReverseList,
                    tls12AvailableWithSha2HashFunctionSelected,
                    tls12AvailableWithWeakCipherSuiteNotSelected,
                    tls11AvailableWithBestCipherSuiteSelected,
                    tls11AvailableWithWeakCipherSuiteNotSelected,
                    tls10AvailableWithBestCipherSuiteSelected,
                    tls10AvailableWithWeakCipherSuiteNotSelected,
                    ssl3FailsWithBadCipherSuite,
                    tlsSecureEllipticCurveSelected,
                    tlsSecureDiffieHellmanGroupSelected,
                    tlsWeakCipherSuitesRejected,
                    certificates.Select(_ => Convert.ToBase64String(_.RawData)).ToList(),
                    new List<SelectedCipherSuite>
                    {
                        new SelectedCipherSuite(TlsTestType.Tls12AvailableWithBestCipherSuiteSelected.ToString(),
                            tls12AvailableWithBestCipherSuiteSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(
                            TlsTestType.Tls12AvailableWithBestCipherSuiteSelectedFromReverseList.ToString(),
                            tls12AvailableWithBestCipherSuiteSelectedFromReverseList?.CipherSuite
                                ?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Tls12AvailableWithSha2HashFunctionSelected.ToString(),
                            tls12AvailableWithSha2HashFunctionSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Tls12AvailableWithWeakCipherSuiteNotSelected.ToString(),
                            tls12AvailableWithWeakCipherSuiteNotSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Tls11AvailableWithBestCipherSuiteSelected.ToString(),
                            tls11AvailableWithBestCipherSuiteSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Tls11AvailableWithWeakCipherSuiteNotSelected.ToString(),
                            tls11AvailableWithWeakCipherSuiteNotSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Tls10AvailableWithBestCipherSuiteSelected.ToString(),
                            tls10AvailableWithBestCipherSuiteSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Tls10AvailableWithWeakCipherSuiteNotSelected.ToString(),
                            tls10AvailableWithWeakCipherSuiteNotSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.Ssl3FailsWithBadCipherSuite.ToString(),
                            ssl3FailsWithBadCipherSuite?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.TlsSecureEllipticCurveSelected.ToString(),
                            tlsSecureEllipticCurveSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.TlsSecureDiffieHellmanGroupSelected.ToString(),
                            tlsSecureDiffieHellmanGroupSelected?.CipherSuite?.ToString()),
                        new SelectedCipherSuite(TlsTestType.TlsWeakCipherSuitesRejected.ToString(),
                            tlsWeakCipherSuitesRejected?.CipherSuite?.ToString())
                    }
                );
        }

        private bool CheckHostNotFound(List<TlsTestResult> results)
        {
            return results.All(_ => _.Result.TlsError == TlsError.HOST_NOT_FOUND);
        }

        private BouncyCastleTlsTestResult ToTestResult(TlsTestResult tlsTestResult)
        {
            return tlsTestResult == null
                ? new BouncyCastleTlsTestResult(null, null, null, null, null, null, null)
                : new BouncyCastleTlsTestResult(tlsTestResult.Result.Version,
                    tlsTestResult.Result.CipherSuite,
                    tlsTestResult.Result.CurveGroup,
                    tlsTestResult.Result.SignatureHashAlgorithm,
                    tlsTestResult.Result.TlsError,
                    tlsTestResult.Result.ErrorDescription, tlsTestResult.Result.SmtpResponses);
        }

        private bool IsErrored(List<TlsTestResult> testResults)
        {
            return testResults.Any(IsErrored);
        }

        private bool IsErrored(TlsTestResult testResult)
        {
            return testResult.Result.TlsError == TlsError.TCP_CONNECTION_FAILED ||
                   testResult.Result.TlsError == TlsError.SESSION_INITIALIZATION_FAILED;
        }
    }
}