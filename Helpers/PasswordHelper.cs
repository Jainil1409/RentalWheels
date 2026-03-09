namespace vehicle_management_system_mvc.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            return password;
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            return password == storedPassword;
        }
    }
}
