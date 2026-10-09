namespace WebDeveloper.Exceptions
{
    public class DomainException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }

        public DomainException(string message, string errorCode = "DOMAIN_ERROR", int statusCode = 400) 
            : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }

    public class UserNotFoundException : DomainException
    {
        public UserNotFoundException(string message = "Không tìm thấy người dùng.") 
            : base(message, "USER_NOT_FOUND", 404) { }
    }

    public class DoctorFullyBookedException : DomainException
    {
        public DoctorFullyBookedException(string message = "Bác sĩ đã kín lịch.") 
            : base(message, "DOCTOR_FULLY_BOOKED", 409) { }
    }

    public class InvalidTokenException : DomainException
    {
        public InvalidTokenException(string message = "Token không hợp lệ hoặc đã hết hạn.") 
            : base(message, "INVALID_TOKEN", 401) { }
    }
}
