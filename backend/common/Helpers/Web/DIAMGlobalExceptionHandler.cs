namespace Common.Helpers.Web;

using Common.Exceptions;
using Common.Exceptions.EDT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class DIAMGlobalExceptionHandler() : IExceptionFilter
{

    public void OnException(ExceptionContext context)
    {
        var statusCode = context.Exception switch
        {
            ParticipantLookupException => StatusCodes.Status404NotFound,
            DIAMGeneralException => StatusCodes.Status400BadRequest,
            DIAMConfigurationException => StatusCodes.Status500InternalServerError,
            DIAMAuthException => StatusCodes.Status403Forbidden,
            DIAMSecurityException => StatusCodes.Status403Forbidden,
            RecordNotFoundException => StatusCodes.Status404NotFound,
            DIAMUserProvisioningException => StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status500InternalServerError

        };
        context.Result = new ObjectResult(new
        {
            error = context.Exception.Message,
            stackTrace = context.Exception.StackTrace
        })
        {
            StatusCode = statusCode
        };
    }
}

