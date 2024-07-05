using E_Commerce.Application.Common.Interfaces;

namespace E_Commerce.Infrastructure.Services;

public class DateTimeService : IDateTime
{
    public DateTime Now => DateTime.Now;
}
