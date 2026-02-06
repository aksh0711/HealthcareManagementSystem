using System.ComponentModel.DataAnnotations;
using HealthcareManagementSystem.Utilities;

namespace HealthcareManagementSystem.Attributes
{
    /// <summary>
    /// Custom validation attribute for phone numbers that uses the PhoneNumberUtility
    /// </summary>
    public class PhoneNumberAttribute : ValidationAttribute
    {
        public PhoneNumberAttribute() : base("Please enter a valid phone number (e.g., (123) 456-7890 or +1234567890)")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return true; // Let Required attribute handle null validation

            var phoneNumber = value.ToString();
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true; // Let Required attribute handle empty validation

            return PhoneNumberUtility.IsValidPhoneNumber(phoneNumber);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field is not a valid phone number. Please use format: (123) 456-7890 or +1234567890";
        }
    }

    /// <summary>
    /// Custom validation attribute for phone numbers that ensures the number can receive SMS
    /// </summary>
    public class MobilePhoneNumberAttribute : PhoneNumberAttribute
    {
        public MobilePhoneNumberAttribute() : base()
        {
        }

        public override bool IsValid(object? value)
        {
            if (!base.IsValid(value))
                return false;

            if (value == null)
                return true;

            var phoneNumber = value.ToString();
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return true;

            return PhoneNumberUtility.IsLikelyMobile(phoneNumber);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"The {name} field must be a valid mobile phone number that can receive SMS messages.";
        }
    }
}
