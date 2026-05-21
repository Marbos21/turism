import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  TripPlan, CreateTripPlanRequest, ItineraryDay, BudgetBreakdown, TripStatus, ActivityType
} from '../models/trip-plan.model';

interface BackendItineraryItem {
  id: string;
  tripPlanId: string;
  dayNumber: number;
  timeSlot: string;
  activityName: string;
  location: string;
  duration: number;
  estimatedCost: number;
  notes?: string;
}

interface BackendDestination {
  id: string;
  name: string;
  country: string;
  imageUrl: string;
  avgCostPerDay: number;
}

interface BackendTripPlan {
  id: string;
  familyId: string;
  title: string;
  destinationId: string;
  destination: BackendDestination | null;
  startDate: string;
  endDate: string;
  estimatedBudget: number;
  status: number;
  createdAt: string;
  itineraryItems: unknown[];
}

@Injectable({ providedIn: 'root' })
export class TripPlanService {
  constructor(private api: ApiService) {}

  getMyTrips(familyId?: number | string): Observable<TripPlan[]> {
    if (!familyId) return of([]);
    return this.api.get<BackendTripPlan[]>('/trip-plans', { familyId: String(familyId) }).pipe(
      map(list => (list ?? []).map(t => this.toFeTripPlan(t)))
    );
  }

  getTripById(id: number | string): Observable<TripPlan> {
    return this.api.get<BackendTripPlan>(`/trip-plans/${id}`).pipe(
      map(t => this.toFeTripPlan(t))
    );
  }

  createTrip(request: CreateTripPlanRequest): Observable<TripPlan> {
    return this.api.post<BackendTripPlan>('/trip-plans', request).pipe(
      map(t => this.toFeTripPlan(t))
    );
  }

  updateTrip(id: number | string, request: Partial<CreateTripPlanRequest>): Observable<TripPlan> {
    return this.api.put<BackendTripPlan>(`/trip-plans/${id}`, request).pipe(
      map(t => this.toFeTripPlan(t))
    );
  }

  deleteTrip(id: number | string): Observable<void> {
    return this.api.delete<void>(`/trip-plans/${id}`);
  }

  generateItinerary(tripId: number | string): Observable<ItineraryDay[]> {
    return this.api.post<BackendItineraryItem[]>(`/trip-plans/${tripId}/itinerary/generate`, {}).pipe(
      map(items => this.groupIntoDays(items ?? [], tripId))
    );
  }

  private groupIntoDays(items: BackendItineraryItem[], tripPlanId: number | string): ItineraryDay[] {
    const bySlot: Record<string, number> = { Morning: 0, Afternoon: 1, Evening: 2 };
    const byDay = new Map<number, BackendItineraryItem[]>();
    for (const it of items) {
      const arr = byDay.get(it.dayNumber) ?? [];
      arr.push(it);
      byDay.set(it.dayNumber, arr);
    }
    return Array.from(byDay.entries())
      .sort(([a], [b]) => a - b)
      .map(([dayNumber, list]) => {
        list.sort((a, b) => (bySlot[a.timeSlot] ?? 99) - (bySlot[b.timeSlot] ?? 99));
        const activities = list.map((it, idx) => this.toActivity(it, idx));
        return {
          id: dayNumber as unknown as number,
          tripPlanId: tripPlanId as unknown as number,
          dayNumber,
          date: new Date().toISOString(),
          title: dayNumber === 1 ? 'Arrival Day' : `Day ${dayNumber} Exploration`,
          activities,
          totalCostUsd: activities.reduce((s, a) => s + a.costPerPersonUsd, 0)
        };
      });
  }

  private toActivity(it: BackendItineraryItem, idx: number) {
    const start = this.timeForSlot(it.timeSlot, idx);
    const end = this.addMinutes(start, it.duration ?? 60);
    return {
      id: it.id as unknown as number,
      itineraryDayId: 0,
      name: it.activityName,
      description: it.notes ?? '',
      startTime: start,
      endTime: end,
      type: this.deriveType(it.activityName),
      costPerPersonUsd: Number(it.estimatedCost ?? 0),
      location: it.location ?? '',
      isMandatory: false,
      isKidFriendly: true
    };
  }

  private timeForSlot(slot: string, idx: number): string {
    const base = slot === 'Morning' ? 8 : slot === 'Afternoon' ? 13 : 19;
    const h = base + idx;
    return `${String(h).padStart(2, '0')}:00`;
  }

  private addMinutes(time: string, mins: number): string {
    const [h, m] = time.split(':').map(Number);
    const total = h * 60 + m + mins;
    const hh = Math.floor(total / 60) % 24;
    const mm = total % 60;
    return `${String(hh).padStart(2, '0')}:${String(mm).padStart(2, '0')}`;
  }

  private deriveType(name: string): ActivityType {
    const n = name.toLowerCase();
    if (n.includes('hotel') || n.includes('check-in') || n.includes('accommodation')) return ActivityType.Accommodation;
    if (n.includes('breakfast') || n.includes('lunch') || n.includes('dinner') || n.includes('restaurant') || n.includes('meal') || n.includes('food')) return ActivityType.Meal;
    if (n.includes('transport') || n.includes('transfer') || n.includes('airport')) return ActivityType.Transport;
    if (n.includes('shop')) return ActivityType.Shopping;
    if (n.includes('beach') || n.includes('park') || n.includes('walk') || n.includes('viewpoint') || n.includes('free')) return ActivityType.Recreation;
    return ActivityType.Attraction;
  }

  getBudgetEstimate(_tripId: number | string): Observable<BudgetBreakdown> {
    return of({
      accommodationUsd: 0, transportUsd: 0, foodUsd: 0,
      activitiesUsd: 0, otherUsd: 0, totalUsd: 0,
      numberOfPeople: 0, perPersonUsd: 0
    });
  }

  private toFeTripPlan(be: BackendTripPlan): TripPlan {
    const dest = be.destination;
    const start = new Date(be.startDate);
    const end = new Date(be.endDate);
    const ms = end.getTime() - start.getTime();
    const durationDays = Number.isFinite(ms) ? Math.max(1, Math.round(ms / (1000 * 60 * 60 * 24))) : 0;
    return {
      id: be.id as unknown as number,
      userId: 0 as unknown as number,
      familyId: be.familyId as unknown as number,
      familyName: '',
      destinationId: be.destinationId as unknown as number,
      destinationName: dest?.name ?? '',
      destinationCountry: dest?.country ?? '',
      destinationImageUrl: dest?.imageUrl ?? '',
      title: be.title ?? '',
      startDate: be.startDate,
      endDate: be.endDate,
      durationDays,
      status: this.statusIntToEnum(be.status),
      totalBudgetUsd: Number(be.estimatedBudget ?? 0),
      createdAt: be.createdAt
    };
  }

  private statusIntToEnum(value: number): TripStatus {
    switch (value) {
      case 1: return TripStatus.Draft;
      case 2: return TripStatus.Planned;
      case 3: return TripStatus.Active;
      case 4: return TripStatus.Completed;
      default: return TripStatus.Draft;
    }
  }
}
