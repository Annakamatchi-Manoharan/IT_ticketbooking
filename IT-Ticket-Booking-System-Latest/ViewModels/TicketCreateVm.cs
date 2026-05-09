using ITBookingSystem.DTOs;
using ITBookingSystem.Models;

namespace ITBookingSystem.ViewModels;

public class TicketCreateVm
{
    public TicketCreateDto Ticket { get; set; } = new();
    public List<IFormFile>? Attachments { get; set; }
}
