//using System;
//using System.Collections.Generic;
//using System.Linq;
//
//namespace MailCheck.Mx.TlsEvaluator.Rules.CertificateEvaluation.Domain
//{
//    public class CertificateEvaluatorResults
//    {
//        public CertificateEvaluatorResults(string hostName, HostResults hostResults, DateTime? endDate = null)
//        {
//            HostName = hostName;
//            HostResults = hostResults;
//            EndDate = endDate;
//        }
//
//        public ulong? HostName { get; }
//        public DateTime? EndDate { get; }
//        
//        public List<HostResults> HostResults { get; }
//        
//
//        protected bool Equals(CertificateEvaluatorResults other)
//        {
//            return HostName == other.HostName && EndDate.Equals(other.EndDate) && &&
//                   HostResults.Equals(other.HostResults);
//        }
//
//        public override bool Equals(object obj)
//        {
//            if (ReferenceEquals(null, obj)) return false;
//            if (ReferenceEquals(this, obj)) return true;
//            if (obj.GetType() != this.GetType()) return false;
//            return Equals((CertificateEvaluatorResults) obj);
//        }
//
//        public override int GetHashCode()
//        {
//            unchecked
//            {
//                var hashCode = HostName.GetHashCode();
//                hashCode = (hashCode * 397) ^ EndDate.GetHashCode();
//                hashCode = (hashCode * 397) ^ (DomainName != null ? DomainName.GetHashCode() : 0);
//                hashCode = (hashCode * 397) ^ (HostResults != null ? HostResults.GetHashCode() : 0);
//                return hashCode;
//            }
//        }
//    }
//}