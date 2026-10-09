namespace Servexa.Domain.Platform.Constants;

/// <summary>
/// Canonical capability permissions defined in the Servexa Platform catalogue.
/// Released with the application; tenants cannot invent permissions.
/// </summary>
public static class Capabilities
{
    // Customer Account Capabilities
    public const string CustomerView = "Customer.View";
    public const string CustomerCreate = "Customer.Create";
    public const string CustomerUpdate = "Customer.Update";

    // Operational Site Capabilities
    public const string SiteView = "Site.View";
    public const string SiteCreate = "Site.Create";
    public const string SiteUpdate = "Site.Update";

    // Service & Work Order Capabilities
    public const string WorkOrderView = "WorkOrder.View";
    public const string WorkOrderCreate = "WorkOrder.Create";
    public const string WorkOrderComplete = "WorkOrder.Complete";

    // Scheduling Capabilities
    public const string BookingView = "Booking.View";
    public const string BookingCreate = "Booking.Create";
    public const string BookingAssign = "Booking.Assign";

    // Billing & Commercial Capabilities
    public const string InvoiceView = "Invoice.View";
    public const string InvoicePost = "Invoice.Post";
}
