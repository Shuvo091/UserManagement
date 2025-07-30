using System.Net;

namespace CohesionX.UserManagement.Application.Exceptions;

/// <summary>
/// Represents a structured application-level exception with optional HTTP status code.
/// </summary>
public class CustomException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CustomException"/> class with a specified error message and HTTP status code.
	/// </summary>
	/// <param name="message"> message. </param>
	/// <param name="statusCode"> code. </param>
	public CustomException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
		: base(message)
	{
		StatusCode = statusCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CustomException"/> class with a specified error message, an inner exception, and an HTTP status code.
	/// </summary>
	/// <param name="message"> message. </param>
	/// <param name="inner"> inner. </param>
	/// <param name="statusCode"> code. </param>
	public CustomException(string message, Exception inner, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
		: base(message, inner)
	{
		StatusCode = statusCode;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CustomException"/> class with no error message or HTTP status code.
	/// </summary>
	public CustomException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CustomException"/> class with a specified error message.
	/// </summary>
	/// <param name="message"> message. </param>
	public CustomException(string? message)
		: base(message)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CustomException"/> class with a specified error message and an inner exception.
	/// </summary>
	/// <param name="message"> message. </param>
	/// <param name="innerException"> Inner exception. </param>
	public CustomException(string? message, Exception? innerException)
		: base(message, innerException)
	{
	}

	/// <summary>
	/// Gets the HTTP status code associated with this exception.
	/// </summary>
	public HttpStatusCode StatusCode { get; }
}
