namespace MES.Application.Common.Exceptions;
public sealed class DataAccessException(string message, Exception? innerException = null) : Exception(message, innerException);
