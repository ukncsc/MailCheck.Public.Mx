using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MailCheck.Mx.Api.Util
{
    public static class ModelStateExtensionMethods
    {
        public static string GetErrorString(this ModelStateDictionary modelState)
        {
            return string.Join(",", modelState.Values.SelectMany(v => v.Errors.Select(_ => _.ErrorMessage)));
        }
    }
}