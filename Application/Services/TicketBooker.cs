using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Application.Dtos;
using Application.Mappers;
using Domain.Entities;

namespace Application.Services;

public class TicketBooker : IBookingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TicketBooker> _logger;
    
    public TicketBooker(IUnitOfWork unitOfWork, ILogger<TicketBooker> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<Ticket?> BookTicketAsync(CreateTicketDto ticketDto)
    {
        try
        {
            var session = await _unitOfWork.Sessions.GetSessionByIdAsync(ticketDto.SessionId);

            if (session == null)
                throw new Exception("Session not found");

            if (session.IsFull())
                throw new Exception($"Session is full, total seats {session.TotalSeats}");

            if (!session.IsSeatExist(ticketDto.SeatNumber))
                throw new Exception(
                    $"Seat {ticketDto.SeatNumber} doesnt exist, total seats: {session.TotalSeats}");

            if (await _unitOfWork.Tickets.CheckSeatsAvailabilityAsync(ticketDto.SeatNumber))
                throw new Exception($"Seat {ticketDto.SeatNumber} is already booked");

            var ticketModel = ticketDto.ToCreateTicketDto();
            
            try
            {
                var ticket = await _unitOfWork.Tickets.CreateTicketAsync(ticketModel);

                return ticket;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw new Exception($"Ticket creation failed: {e.Message}");
            }
        }
        catch (ArgumentOutOfRangeException ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new Exception($"Problems with : {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new Exception($"Error while booking ticket: {ex.Message}");
        }
    }
}