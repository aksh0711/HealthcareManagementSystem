namespace HealthcareManagementSystem.ViewModels
{
    /// <summary>
    /// Generic API response model for consistent API responses
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates if the request was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Human-readable message about the result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The actual data being returned
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// List of validation errors (if any)
        /// </summary>
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Timestamp of the response
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Create a successful response
        /// </summary>
        public static ApiResponse<T> SuccessResponse(T data, string message = "Success")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Create an error response
        /// </summary>
        public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }

    /// <summary>
    /// Non-generic API response for simple success/error messages
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        /// <summary>
        /// Create a simple success response
        /// </summary>
        public static ApiResponse SuccessResult(string message = "Success")
        {
            var response = new ApiResponse();
            response.Success = true;
            response.Message = message;
            return response;
        }

        /// <summary>
        /// Create a simple error response
        /// </summary>
        public static ApiResponse Error(string message, List<string>? errors = null)
        {
            var response = new ApiResponse();
            response.Success = false;
            response.Message = message;
            response.Errors = errors;
            return response;
        }
    }
}
