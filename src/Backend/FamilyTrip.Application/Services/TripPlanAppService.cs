using AutoMapper;
using FamilyTrip.Application.DTOs.Destination;
using FamilyTrip.Application.DTOs.TripPlan;
using FamilyTrip.Application.Integrations;
using FamilyTrip.Domain.Entities;
using FamilyTrip.Domain.Interfaces;

namespace FamilyTrip.Application.Services;

public class TripPlanAppService : ITripPlanAppService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IItineraryOptimizer _itineraryOptimizer;
    private readonly ITripSupplementsService _supplements;

    public TripPlanAppService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IItineraryOptimizer itineraryOptimizer,
        ITripSupplementsService supplements)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _itineraryOptimizer = itineraryOptimizer;
        _supplements = supplements;
    }

    private async Task<int> GetTravelerCountAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var members = await _unitOfWork.FamilyMembers.FindAsync(m => m.FamilyId == familyId, cancellationToken);
        return Math.Max(1, members.Count());
    }

    public async Task<List<TripPlanDto>> GetByFamilyAsync(Guid familyId, Guid userId, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(familyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var travelers = await GetTravelerCountAsync(familyId, cancellationToken);
        var plans = await _unitOfWork.TripPlans.FindAsync(tp => tp.FamilyId == familyId, cancellationToken);
        var result = new List<TripPlanDto>();

        foreach (var plan in plans)
        {
            var dto = _mapper.Map<TripPlanDto>(plan);
            dto.NumberOfTravelers = travelers;
            var destination = await _unitOfWork.Destinations.GetByIdAsync(plan.DestinationId, cancellationToken);
            if (destination is not null)
                dto.Destination = _mapper.Map<DestinationDto>(destination);

            var items = await _unitOfWork.ItineraryItems.FindAsync(i => i.TripPlanId == plan.Id, cancellationToken);
            dto.ItineraryItems = _mapper.Map<List<ItineraryItemDto>>(items.OrderBy(i => i.DayNumber).ThenBy(i => i.TimeSlot).ToList());
            result.Add(dto);
        }

        return result;
    }

    public async Task<TripPlanDto> CreateAsync(Guid userId, CreateTripPlanRequest request, CancellationToken cancellationToken = default)
    {
        var family = await _unitOfWork.Families.GetByIdAsync(request.FamilyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new KeyNotFoundException("Family not found.");

        var destination = await _unitOfWork.Destinations.GetByIdAsync(request.DestinationId, cancellationToken);
        if (destination is null)
            throw new KeyNotFoundException("Destination not found.");

        var plan = new TripPlan
        {
            Id = Guid.NewGuid(),
            FamilyId = request.FamilyId,
            Title = request.Title,
            DestinationId = request.DestinationId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            EstimatedBudget = request.EstimatedBudget,
            Status = Domain.Enums.TripStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TripPlans.AddAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TripPlanDto>(plan);
        dto.Destination = _mapper.Map<DestinationDto>(destination);
        dto.NumberOfTravelers = await GetTravelerCountAsync(request.FamilyId, cancellationToken);
        dto.ItineraryItems = new List<ItineraryItemDto>();
        return dto;
    }

    public async Task<TripPlanDto> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var plan = await _unitOfWork.TripPlans.GetByIdAsync(id, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException("Trip plan not found.");

        var family = await _unitOfWork.Families.GetByIdAsync(plan.FamilyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        var dto = _mapper.Map<TripPlanDto>(plan);
        var destination = await _unitOfWork.Destinations.GetByIdAsync(plan.DestinationId, cancellationToken);
        if (destination is not null)
            dto.Destination = _mapper.Map<DestinationDto>(destination);
        dto.NumberOfTravelers = await GetTravelerCountAsync(plan.FamilyId, cancellationToken);

        var items = await _unitOfWork.ItineraryItems.FindAsync(i => i.TripPlanId == plan.Id, cancellationToken);
        dto.ItineraryItems = _mapper.Map<List<ItineraryItemDto>>(items.OrderBy(i => i.DayNumber).ThenBy(i => i.TimeSlot).ToList());
        return dto;
    }

    public async Task<TripPlanDto> UpdateAsync(Guid id, Guid userId, UpdateTripPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _unitOfWork.TripPlans.GetByIdAsync(id, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException("Trip plan not found.");

        var family = await _unitOfWork.Families.GetByIdAsync(plan.FamilyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        plan.Title = request.Title;
        plan.StartDate = request.StartDate;
        plan.EndDate = request.EndDate;
        plan.EstimatedBudget = request.EstimatedBudget;
        plan.Status = request.Status;

        await _unitOfWork.TripPlans.UpdateAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TripPlanDto>(plan);
        var destination = await _unitOfWork.Destinations.GetByIdAsync(plan.DestinationId, cancellationToken);
        if (destination is not null)
            dto.Destination = _mapper.Map<DestinationDto>(destination);
        dto.NumberOfTravelers = await GetTravelerCountAsync(plan.FamilyId, cancellationToken);

        var items = await _unitOfWork.ItineraryItems.FindAsync(i => i.TripPlanId == plan.Id, cancellationToken);
        dto.ItineraryItems = _mapper.Map<List<ItineraryItemDto>>(items.OrderBy(i => i.DayNumber).ThenBy(i => i.TimeSlot).ToList());
        return dto;
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var plan = await _unitOfWork.TripPlans.GetByIdAsync(id, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException("Trip plan not found.");

        var family = await _unitOfWork.Families.GetByIdAsync(plan.FamilyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        var items = await _unitOfWork.ItineraryItems.FindAsync(i => i.TripPlanId == plan.Id, cancellationToken);
        foreach (var item in items)
            await _unitOfWork.ItineraryItems.DeleteAsync(item, cancellationToken);

        await _unitOfWork.TripPlans.DeleteAsync(plan, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<ItineraryItemDto>> GenerateItineraryAsync(Guid tripPlanId, Guid userId, CancellationToken cancellationToken = default)
    {
        var plan = await _unitOfWork.TripPlans.GetByIdAsync(tripPlanId, cancellationToken);
        if (plan is null)
            throw new KeyNotFoundException("Trip plan not found.");

        var family = await _unitOfWork.Families.GetByIdAsync(plan.FamilyId, cancellationToken);
        if (family is null || family.UserId != userId)
            throw new UnauthorizedAccessException("Access denied.");

        var destination = await _unitOfWork.Destinations.GetByIdAsync(plan.DestinationId, cancellationToken);
        if (destination is null)
            throw new KeyNotFoundException("Destination not found.");

        var members = await _unitOfWork.FamilyMembers.FindAsync(m => m.FamilyId == plan.FamilyId, cancellationToken);

        // Remove existing itinerary items
        var existingItems = await _unitOfWork.ItineraryItems.FindAsync(i => i.TripPlanId == tripPlanId, cancellationToken);
        foreach (var item in existingItems)
            await _unitOfWork.ItineraryItems.DeleteAsync(item, cancellationToken);

        // Generate activities + travel/accommodation in parallel
        var membersList = members.ToList();
        // Treat every family member as a traveller (kids fly + eat too). For an
        // empty family default to 2 so the math doesn't collapse.
        var travelers = Math.Max(1, membersList.Count == 0 ? 2 : membersList.Count);

        var itineraryTask = _itineraryOptimizer.GenerateItineraryAsync(plan, destination, family, membersList, cancellationToken);
        var supplementsTask = _supplements.ResolveAsync(
            plan.StartDate, plan.EndDate,
            destination.Latitude, destination.Longitude,
            destination.Name, destination.Country,
            travelers, cancellationToken);

        await Task.WhenAll(itineraryTask, supplementsTask);
        var generatedItems = itineraryTask.Result;
        var supplements = supplementsTask.Result;

        var totalDays = Math.Max(1, (int)(plan.EndDate - plan.StartDate).TotalDays);

        // Outbound flight on day 1 morning, before activities. We quoted Duffel
        // for `travelers` passengers, so dividing the total by travelers gives
        // a per-traveller figure consistent with the activity costs.
        if (supplements.Outbound is { } outbound)
        {
            generatedItems.Insert(0, new ItineraryItem
            {
                TripPlanId = tripPlanId,
                DayNumber = 1,
                TimeSlot = "Morning",
                ActivityName = $"Flight {outbound.OriginIata} → {outbound.DestinationIata}",
                Location = $"{outbound.OriginIata} airport",
                Duration = Math.Max(60, (int)(outbound.ArrivalAt - outbound.DepartureAt).TotalMinutes),
                EstimatedCost = Math.Round(outbound.TotalAmount / travelers, 2),
                Notes = $"{outbound.Airline} · departs {outbound.DepartureAt:HH:mm} · {outbound.Currency} {outbound.TotalAmount:0} total for {travelers} traveller(s)"
            });
        }

        // Nightly accommodation row on each day. Prefer the real Duffel rate;
        // fall back to a heuristic (40% of the destination's daily cost estimate)
        // so the user still sees a representative lodging line even when the
        // sandbox returns no priced offers.
        var realNightly = supplements.Hotels?.CheapestNightlyAmount;
        var hotelName = supplements.Hotels?.CheapestHotelName
                        ?? $"Hotel in {destination.Name} (estimated)";
        var nightlyAmount = realNightly ?? Math.Round(destination.AvgCostPerDay * 0.40m, 2);
        var nightlyNotes = realNightly is not null
            ? $"Cheapest available rate via Duffel{(string.IsNullOrWhiteSpace(supplements.Hotels?.Currency) ? "" : $" ({supplements.Hotels.Currency})")}"
            : "Estimated nightly rate based on destination cost tier";

        if (nightlyAmount > 0)
        {
            for (var day = 1; day <= totalDays; day++)
            {
                generatedItems.Add(new ItineraryItem
                {
                    TripPlanId = tripPlanId,
                    DayNumber = day,
                    TimeSlot = "Evening",
                    ActivityName = $"Stay at {hotelName}",
                    Location = hotelName,
                    Duration = 600,
                    EstimatedCost = nightlyAmount,
                    Notes = nightlyNotes
                });
            }
        }

        // Return flight on the last day evening
        if (supplements.Return is { } ret && totalDays >= 1)
        {
            generatedItems.Add(new ItineraryItem
            {
                TripPlanId = tripPlanId,
                DayNumber = totalDays,
                TimeSlot = "Evening",
                ActivityName = $"Flight {ret.OriginIata} → {ret.DestinationIata}",
                Location = $"{ret.OriginIata} airport",
                Duration = Math.Max(60, (int)(ret.ArrivalAt - ret.DepartureAt).TotalMinutes),
                EstimatedCost = Math.Round(ret.TotalAmount / travelers, 2),
                Notes = $"{ret.Airline} · departs {ret.DepartureAt:HH:mm} · {ret.Currency} {ret.TotalAmount:0} total for {travelers} traveller(s)"
            });
        }

        foreach (var item in generatedItems)
        {
            item.Id = Guid.NewGuid();
            item.TripPlanId = tripPlanId;
            item.CreatedAt = DateTime.UtcNow;
            await _unitOfWork.ItineraryItems.AddAsync(item, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<List<ItineraryItemDto>>(generatedItems.OrderBy(i => i.DayNumber).ThenBy(i => i.TimeSlot).ToList());
    }
}
