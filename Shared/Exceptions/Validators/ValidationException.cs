using System.Net;

namespace Shared.Exceptions.Validators;

public abstract class ValidationException : Exception {
    public abstract string ErrorCode { get; }
    
    public virtual HttpStatusCode HttpStatusCode => HttpStatusCode.BadRequest;
    
    protected ValidationException(string message) : base(message) { }
    
    protected ValidationException(string message, Exception inner) : base(message, inner) { }
}