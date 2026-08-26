namespace EduCore.Enums;

// Why an enrollment ended. Only meaningful once Enrollment.LeftAt is set.
public enum EnrollmentExitReason
{
    TransferredOut, Withdrawn, Graduated, EndOfYear
}
