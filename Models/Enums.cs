namespace ProVMSIT15.Models;

public enum UserRole
{
    Admin,
    Procurement,
    Finance,
    User,
    Vendor
}

public enum OperationalStatus
{
    PendingVerification,
    Active,
    Suspended,
    Blacklisted
}

public enum ItemCategory
{
    IT_Hardware,
    Office_Facilities,
    Marketing_Collateral
}

public enum WorkflowStatus
{
    Pending_Finance,
    Approved_Budget,
    PO_Issued,
    In_Transit,
    Delivered,
    Archived
}

public enum ContractStatus
{
    Draft,
    UnderNegotiation,
    Active,
    Expiring,
    Expired,
    Terminated
}
