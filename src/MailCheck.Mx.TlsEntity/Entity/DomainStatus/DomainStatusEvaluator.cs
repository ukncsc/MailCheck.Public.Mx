using System.Collections.Generic;
using System.Linq;
using MailCheck.DomainStatus.Contracts;
using MailCheck.Mx.Contracts.SharedDomain;

namespace MailCheck.Mx.TlsEntity.Entity.DomainStatus
{
    public interface IDomainStatusEvaluator
    {
        Status GetStatus(List<TlsEvaluatedResult> evaluatedResults, List<Error> certificateErrors);
    }

    public class DomainStatusEvaluator : IDomainStatusEvaluator
    {
        public Status GetStatus(List<TlsEvaluatedResult> evaluatedResults, List<Error> certificateErrors)
        {
            List<EvaluatorResult?> evaluatorResults = evaluatedResults?.Select(x => x.Result).ToList();

            Status status = Status.Success;

            if (evaluatorResults != null && evaluatorResults.Any(x => x == EvaluatorResult.FAIL) ||
                certificateErrors !=null  && certificateErrors.Any(x => x.ErrorType == ErrorType.Error))
            {
                status = Status.Error;
            }
            else if (evaluatorResults != null && evaluatorResults.Any(x => x == EvaluatorResult.WARNING) ||
                     certificateErrors != null && certificateErrors.Any(x => x.ErrorType == ErrorType.Warning))
            {
                status = Status.Warning;
            }
            else if (evaluatorResults != null &&  (evaluatorResults.Any(x => x == EvaluatorResult.INCONCLUSIVE || x == EvaluatorResult.INFORMATIONAL || x == EvaluatorResult.PENDING || x == EvaluatorResult.UNKNOWN)) ||
                     certificateErrors != null && certificateErrors.Any(x => x.ErrorType == ErrorType.Inconclusive))
            {
                status = Status.Info;
            }

            return status;
        }
    }
}