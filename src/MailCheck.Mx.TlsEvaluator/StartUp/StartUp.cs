using Amazon.SimpleNotificationService;
using Amazon.SimpleSystemsManagement;
using MailCheck.Common.Environment.Abstractions;
using MailCheck.Common.Environment.Implementations;
using MailCheck.Common.Messaging.Abstractions;
using MailCheck.Common.SSM;
using MailCheck.Mx.Contracts.Tester;
using MailCheck.Mx.TlsEvaluator.Config;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using MailCheck.Mx.TlsEvaluator.Rules;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.LookUp;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Preprocessors;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Revocation;
using MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Rules;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Ssl3;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls10;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls11;
using MailCheck.Mx.TlsEvaluator.Rules.TlsEvaluation.Tls12;

namespace MailCheck.Mx.TlsEvaluator.StartUp
{
    public class StartUp : IStartUp
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddTransient<IEnvironment, EnvironmentWrapper>()
                .AddTransient<IEnvironmentVariables, EnvironmentVariables>()
                .AddSingleton<IAmazonSimpleSystemsManagement, CachingAmazonSimpleSystemsManagementClient>()
                .AddTransient<IAmazonSimpleNotificationService, AmazonSimpleNotificationServiceClient>()
                .AddTransient<IEvaluationProcessor, EvaluationProcessor>()
                .AddTransient<ITlsRptEvaluatorConfig, TlsRptEvaluatorConfig>()
                .AddTransient<IMxSecurityEvaluator, MxSecurityEvaluator>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Ssl3FailsWithBadCipherSuite>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls10Available>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls10AvailableWithBestCipherSuiteSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls10AvailableWithWeakCipherSuiteNotSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls11Available>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls11AvailableWithBestCipherSuiteSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls11AvailableWithWeakCipherSuiteNotSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls12Available>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls12AvailableWithBestCipherSuiteSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls12AvailableWithBestCipherSuiteSelectedFromReverseList>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls12AvailableWithSha2HashFunctionSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, Tls12AvailableWithWeakCipherSuiteNotSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, TlsSecureDiffieHellmanGroupSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult >, TlsSecureEllipticCurveSelected>()
                .AddTransient<IRule<TlsTestResults, RuleTypedTlsEvaluationResult>, TlsWeakCipherSuitesRejected>()
                .AddTransient<IEvaluator<TlsTestResults, RuleTypedTlsEvaluationResult>, Evaluator<TlsTestResults, RuleTypedTlsEvaluationResult>>()
                .AddTransient<IEvaluator<HostCertificates>, CertificateEvaluator>()
                .AddTransient<IRule<HostCertificates>, AllCertificatesShouldBeInOrder>()
                .AddTransient<IRule<HostCertificates>, AllCertificatesShouldBePresent>()
                .AddTransient<IRule<HostCertificates>, AllCertificatesShouldHaveStrongKey>()
                .AddTransient<IRule<HostCertificates>, AllCertificatesSignaturesShouldBeValid>()
                .AddTransient<IRule<HostCertificates>, CertificateExpiryShouldBeInDate>()
                .AddTransient<IRule<HostCertificates>, CertificateShouldMatchHostName>()
                .AddTransient<IRule<HostCertificates>, HostShouldHaveCertificates>()
                .AddTransient<IRule<HostCertificates>, NonRootCertificatesShouldNotAppearOnRevocationLists>()
                .AddTransient<IRule<HostCertificates>, RootCertificateShouldBeTrusted>()
                .AddTransient<IRule<HostCertificates>, RootAndIntermediateCertificatesMustHaveKeyCertSign>()
                .AddTransient<IRule<HostCertificates>, LeafCertificateMustHaveCorrectExtendedKeyUsage>()
                .AddTransient<IRule<HostCertificates>, LeafCertificateMustHaveCorrectKeyUsage>()
                .AddTransient<IOcspValidator, OcspValidator>()
                .AddTransient<ICrlValidator, CrlValidator>()
                .AddTransient<IPreprocessorComposite<HostCertificates>, CertificatePreprocessor>()
                .AddTransient<IPreprocessor<HostCertificates>, EnsureRootCertificatePreprocessor>()
                .AddTransient<IRootCertificateProvider, MozillaRootCertificateProvider>()
                .AddTransient<IClock, Clock>()
                .AddSingleton<IRootCertificateLookUp, RootCertificateLookUp>()
                .AddTransient<ICertificateEvaluatorHandler, CertificateEvaluatorHandler>()
                .AddTransient<EvaluationHandler>();
        }
    }
}
