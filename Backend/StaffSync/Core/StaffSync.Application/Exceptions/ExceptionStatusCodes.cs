using FluentValidation;
using Microsoft.AspNetCore.Http;
using StaffSync.Application.Bases;

namespace StaffSync.Application.Exceptions
{
    public static class ExceptionStatusCodes
    {
        // Sıra önemli: NotFoundException bir BaseException'dır, önce kontrol edilmeli.
        public static int For(Exception exception) => exception switch
        {
            ValidationException => StatusCodes.Status422UnprocessableEntity,
            NotFoundException => StatusCodes.Status404NotFound,
            BaseException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
