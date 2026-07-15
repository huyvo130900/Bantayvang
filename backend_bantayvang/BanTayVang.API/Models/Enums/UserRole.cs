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
        /// Teacher/Instructor - OBSOLETE, no longer used
        /// </summary>
        [System.Obsolete("Teacher role is no longer used. Use DeptManager instead.")]
        Teacher = 2,

        /// <summary>
        /// Student - can take exams
        /// </summary>
        Student = 3,

        /// <summary>
        /// Exam supervisor - OBSOLETE, no longer used
        /// </summary>
        [System.Obsolete("Supervisor role is no longer used. Use DeptManager instead.")]
        Supervisor = 4,

        /// <summary>
        /// Department Manager - manages questions and exams for their department
        /// </summary>
        DeptManager = 5,

        /// <summary>
        /// External candidate
        /// </summary>
        ThiSinhNgoai = 6
    }

    /// <summary>
    /// User account status
    /// </summary>
    public enum UserStatus
    {
        Active = 1,
        Suspended = 2,
        Deactivated = 3,
        Pending = 4
    }
}
