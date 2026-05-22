namespace BanTayVang.API.Models.Enums
{
    /// <summary>
    /// User roles for role-based authorization
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// System administrator - full access
        /// </summary>
        Admin = 1,

        /// <summary>
        /// Teacher/Instructor - can create and manage exams
        /// </summary>
        Teacher = 2,

        /// <summary>
        /// Student - can take exams
        /// </summary>
        Student = 3,

        /// <summary>
        /// Exam supervisor - can monitor exams
        /// </summary>
        Supervisor = 4
    }

    /// <summary>
    /// User account status
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// Account is active and can be used
        /// </summary>
        Active = 1,

        /// <summary>
        /// Account is temporarily suspended
        /// </summary>
        Suspended = 2,

        /// <summary>
        /// Account is permanently deactivated
        /// </summary>
        Deactivated = 3,

        /// <summary>
        /// Account is pending activation
        /// </summary>
        Pending = 4
    }
}