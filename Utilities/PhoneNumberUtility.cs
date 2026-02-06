using System.Text.RegularExpressions;

namespace HealthcareManagementSystem.Utilities
{
    public static class PhoneNumberUtility
    {
        // Regex patterns for phone number validation
        private static readonly Regex UsPhonePattern = new Regex(@"^(\+?1[-.\s]?)?\(?([0-9]{3})\)?[-.\s]?([0-9]{3})[-.\s]?([0-9]{4})$");
        private static readonly Regex IndianPhonePattern = new Regex(@"^(\+?91[-.\s]?)?[6-9]\d{9}$");
        private static readonly Regex IndianMobilePattern = new Regex(@"^(\+?91[-.\s]?)?[6-9]\d{9}$");
        private static readonly Regex InternationalPattern = new Regex(@"^\+[1-9]\d{1,14}$");
        private static readonly Regex DigitsOnlyPattern = new Regex(@"[^\d]");

        /// <summary>
        /// Validates if a phone number is in a valid format (supports US and Indian numbers)
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Check if it's already in international format
            if (InternationalPattern.IsMatch(phoneNumber))
                return true;

            // Check if it's a valid US phone number
            if (UsPhonePattern.IsMatch(phoneNumber))
                return true;

            // Check if it's a valid Indian phone number
            return IndianPhonePattern.IsMatch(phoneNumber);
        }

        /// <summary>
        /// Formats a phone number to international format for SMS
        /// Supports US (+1) and Indian (+91) numbers
        /// </summary>
        /// <param name="phoneNumber">Phone number to format</param>
        /// <param name="defaultCountryCode">Default country code (default: +91 for India)</param>
        /// <returns>Formatted phone number in international format</returns>
        public static string FormatForSms(string phoneNumber, string defaultCountryCode = "+91")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException("Phone number cannot be empty", nameof(phoneNumber));

            // Remove all non-digit characters
            string digitsOnly = DigitsOnlyPattern.Replace(phoneNumber, "");

            // If already starts with country code, add + if missing
            if (phoneNumber.StartsWith("+"))
            {
                return phoneNumber;
            }

            // If starts with country code without +, add it
            if (digitsOnly.StartsWith("91") && digitsOnly.Length == 12)
            {
                return $"+{digitsOnly}";
            }

            if (digitsOnly.StartsWith("1") && digitsOnly.Length == 11)
            {
                return $"+{digitsOnly}";
            }

            // If it's a 10-digit Indian number starting with 6-9
            if (digitsOnly.Length == 10 && digitsOnly[0] >= '6' && digitsOnly[0] <= '9')
            {
                return $"+91{digitsOnly}";
            }

            // If it's a 10-digit number (assume US if starts with 2-5)
            if (digitsOnly.Length == 10)
            {
                return $"+1{digitsOnly}";
            }

            // If it's an 11-digit number starting with 1 (US with country code)
            if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                return $"+{digitsOnly}";
            }

            // For other cases, assume it needs the default country code
            return $"{defaultCountryCode}{digitsOnly}";
        }

        /// <summary>
        /// Formats a phone number for display (supports US and Indian formats)
        /// US format: (123) 456-7890, Indian format: +91 98765 43210
        /// </summary>
        /// <param name="phoneNumber">Phone number to format</param>
        /// <returns>Formatted phone number for display</returns>
        public static string FormatForDisplay(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return "";

            // Remove all non-digit characters
            string digitsOnly = DigitsOnlyPattern.Replace(phoneNumber, "");

            // Format Indian phone numbers (10 digits starting with 6-9)
            if (digitsOnly.Length == 10 && digitsOnly[0] >= '6' && digitsOnly[0] <= '9')
            {
                return $"+91 {digitsOnly.Substring(0, 5)} {digitsOnly.Substring(5, 5)}";
            }
            // Format Indian phone numbers with country code (12 digits starting with 91)
            else if (digitsOnly.Length == 12 && digitsOnly.StartsWith("91"))
            {
                return $"+91 {digitsOnly.Substring(2, 5)} {digitsOnly.Substring(7, 5)}";
            }
            // Format US phone numbers (10 digits)
            else if (digitsOnly.Length == 10)
            {
                return $"({digitsOnly.Substring(0, 3)}) {digitsOnly.Substring(3, 3)}-{digitsOnly.Substring(6, 4)}";
            }
            // Format US phone numbers with country code (11 digits starting with 1)
            else if (digitsOnly.Length == 11 && digitsOnly.StartsWith("1"))
            {
                return $"+1 ({digitsOnly.Substring(1, 3)}) {digitsOnly.Substring(4, 3)}-{digitsOnly.Substring(7, 4)}";
            }
            else
            {
                // International number - just add + if missing
                return phoneNumber.StartsWith("+") ? phoneNumber : $"+{digitsOnly}";
            }
        }

        /// <summary>
        /// Validates and returns a clean phone number, or throws an exception if invalid
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate and clean</param>
        /// <param name="fieldName">Field name for error messages</param>
        /// <returns>Clean, validated phone number</returns>
        public static string ValidateAndClean(string phoneNumber, string fieldName = "Phone number")
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentException($"{fieldName} is required", nameof(phoneNumber));

            if (!IsValidPhoneNumber(phoneNumber))
                throw new ArgumentException($"{fieldName} is not in a valid format. Please use format: +91 98765 43210, +1234567890, or (123) 456-7890", nameof(phoneNumber));

            return FormatForSms(phoneNumber);
        }

        /// <summary>
        /// Gets the phone number validation error message if invalid, or null if valid
        /// </summary>
        /// <param name="phoneNumber">Phone number to validate</param>
        /// <returns>Error message or null if valid</returns>
        public static string? GetValidationError(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return "Phone number is required";

            if (!IsValidPhoneNumber(phoneNumber))
                return "Please enter a valid phone number (e.g., (123) 456-7890 or +1234567890)";

            return null;
        }

        /// <summary>
        /// Checks if a phone number is likely a mobile number (for SMS compatibility)
        /// This is a basic check - for production, consider using a phone number validation service
        /// </summary>
        /// <param name="phoneNumber">Phone number to check</param>
        /// <returns>True if likely mobile, false otherwise</returns>
        public static bool IsLikelyMobile(string phoneNumber)
        {
            // This is a simplified check - in production, you'd want to use a service like
            // Twilio Lookup API or similar to verify if a number can receive SMS
            
            if (!IsValidPhoneNumber(phoneNumber))
                return false;

            // For now, assume all valid numbers can receive SMS
            // You can enhance this with carrier lookup if needed
            return true;
        }

        /// <summary>
        /// Example phone numbers for testing/documentation
        /// </summary>
        public static class Examples
        {
            public static readonly string[] ValidFormats = {
                "+1234567890",
                "1234567890",
                "(123) 456-7890",
                "123-456-7890",
                "123.456.7890",
                "123 456 7890",
                "+1 (123) 456-7890"
            };

            public static readonly string[] InvalidFormats = {
                "123456789",  // Too short
                "12345678901", // Too long (without proper country code)
                "(123) 456",   // Incomplete
                "abc-def-ghij", // Letters
                "",            // Empty
                "+",           // Just plus sign
                "123"          // Too short
            };
        }
    }
}
