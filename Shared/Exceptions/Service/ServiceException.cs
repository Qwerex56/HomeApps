using System.Net;

namespace Shared.Exceptions.Service;

public abstract class ServiceException : Exception {
    public abstract string ErrorCode { get; }
    
    public virtual HttpStatusCode HttpStatusCode => HttpStatusCode.NotFound;
    
    protected ServiceException(string value) : base(value) {}
    protected ServiceException(string value, Exception innerException) : base(value, innerException) {}
}