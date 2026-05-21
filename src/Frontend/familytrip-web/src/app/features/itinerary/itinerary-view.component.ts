import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { TripPlanService } from '../../core/services/trip-plan.service';
import { TripPlan, ItineraryDay, Activity, ActivityType } from '../../core/models/trip-plan.model';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-itinerary-view',
  standalone: true,
  imports: [CommonModule, RouterModule, DateFormatPipe],
  templateUrl: './itinerary-view.component.html',
  styleUrls: ['./itinerary-view.component.scss']
})
export class ItineraryViewComponent implements OnInit {
  private route          = inject(ActivatedRoute);
  private tripPlanService= inject(TripPlanService);

  trip        = signal<TripPlan | null>(null);
  itinerary   = signal<ItineraryDay[]>([]);
  isLoading   = signal(true);
  isGenerating= signal(false);
  activeDay   = signal<number>(1);

  readonly ActivityType = ActivityType;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadTrip(id);
  }

  private loadTrip(id: string): void {
    this.tripPlanService.getTripById(id).subscribe({
      next: (trip) => {
        this.trip.set(trip);
        if (trip.itinerary?.length) {
          this.itinerary.set(trip.itinerary);
          this.isLoading.set(false);
        } else {
          // No stored itinerary yet - have the AI generate one immediately.
          this.runGenerate(trip.id, /* keepLoading */ true);
        }
      },
      error: () => {
        this.trip.set(null);
        this.itinerary.set([]);
        this.isLoading.set(false);
      }
    });
  }

  generateItinerary(): void {
    const t = this.trip();
    if (!t) return;
    this.runGenerate(t.id, /* keepLoading */ false);
  }

  private runGenerate(tripId: number | string, keepLoading: boolean): void {
    this.isGenerating.set(true);
    this.tripPlanService.generateItinerary(tripId).subscribe({
      next: (days) => {
        this.itinerary.set(days);
        this.isGenerating.set(false);
        if (keepLoading) this.isLoading.set(false);
      },
      error: () => {
        this.itinerary.set([]);
        this.isGenerating.set(false);
        if (keepLoading) this.isLoading.set(false);
      }
    });
  }

  get activeDay$(): ItineraryDay | undefined {
    return this.itinerary().find(d => d.dayNumber === this.activeDay());
  }

  get totalTripCost(): number {
    return this.itinerary().reduce((sum, day) => sum + day.totalCostUsd, 0);
  }

  get totalActivities(): number {
    return this.itinerary().reduce((sum, day) => sum + day.activities.length, 0);
  }

  getActivityTypeIcon(type: ActivityType): string {
    const icons: Record<ActivityType, string> = {
      [ActivityType.Accommodation]: 'hotel',
      [ActivityType.Transport]:     'directions_bus',
      [ActivityType.Meal]:          'restaurant',
      [ActivityType.Attraction]:    'attractions',
      [ActivityType.Shopping]:      'shopping_bag',
      [ActivityType.Recreation]:    'sports',
      [ActivityType.Free]:          'free_cancellation'
    };
    return icons[type] || 'event';
  }

  getActivityTypeColor(type: ActivityType): string {
    const colors: Record<ActivityType, string> = {
      [ActivityType.Accommodation]: '#2196f3',
      [ActivityType.Transport]:     '#ff9800',
      [ActivityType.Meal]:          '#4caf50',
      [ActivityType.Attraction]:    '#9c27b0',
      [ActivityType.Shopping]:      '#e91e63',
      [ActivityType.Recreation]:    '#00bcd4',
      [ActivityType.Free]:          '#607d8b'
    };
    return colors[type] || '#9e9e9e';
  }

  private getMockItinerary(trip: TripPlan): ItineraryDay[] {
    const days: ItineraryDay[] = [];
    const startDate = new Date(trip.startDate);

    const activitiesByDay = [
      [
        { name: 'Hotel Check-in', type: ActivityType.Accommodation, startTime: '15:00', endTime: '16:00', costPerPersonUsd: 0, location: 'Hotel Arts Barcelona', isMandatory: true, isKidFriendly: true },
        { name: 'Welcome Dinner at La Barceloneta', type: ActivityType.Meal, startTime: '19:00', endTime: '21:00', costPerPersonUsd: 35, location: 'Barceloneta Beach', isMandatory: false, isKidFriendly: true }
      ],
      [
        { name: 'Breakfast at the hotel', type: ActivityType.Meal, startTime: '08:00', endTime: '09:00', costPerPersonUsd: 15, location: 'Hotel', isMandatory: false, isKidFriendly: true },
        { name: 'Sagrada Família Tour', type: ActivityType.Attraction, startTime: '10:00', endTime: '13:00', costPerPersonUsd: 26, location: 'Carrer de Mallorca, 401', isMandatory: true, isKidFriendly: true },
        { name: 'Lunch near Sagrada Família', type: ActivityType.Meal, startTime: '13:30', endTime: '14:30', costPerPersonUsd: 20, location: 'L\'Eixample District', isMandatory: false, isKidFriendly: true },
        { name: 'Park Güell Exploration', type: ActivityType.Attraction, startTime: '16:00', endTime: '18:30', costPerPersonUsd: 10, location: 'Carrer d\'Olot, s/n', isMandatory: false, isKidFriendly: true }
      ],
      [
        { name: 'Barceloneta Beach Morning', type: ActivityType.Recreation, startTime: '09:00', endTime: '12:00', costPerPersonUsd: 0, location: 'Barceloneta Beach', isMandatory: false, isKidFriendly: true },
        { name: 'Seafood Lunch', type: ActivityType.Meal, startTime: '13:00', endTime: '14:30', costPerPersonUsd: 30, location: 'Passeig Marítim', isMandatory: false, isKidFriendly: true },
        { name: 'Barcelona Aquarium', type: ActivityType.Attraction, startTime: '16:00', endTime: '18:30', costPerPersonUsd: 22, location: 'Moll d\'Espanya', isMandatory: false, isKidFriendly: true }
      ]
    ];

    for (let i = 0; i < Math.min(trip.durationDays, 7); i++) {
      const date = new Date(startDate);
      date.setDate(startDate.getDate() + i);
      const activities = activitiesByDay[i % activitiesByDay.length];
      const dayActivities: Activity[] = activities.map((a, j) => ({
        id: i * 10 + j,
        itineraryDayId: i + 1,
        description: `Experience ${a.name} in ${trip.destinationName}`,
        latitude: undefined,
        longitude: undefined,
        ...a
      }));
      const totalCost = dayActivities.reduce((s, a) => s + a.costPerPersonUsd * 4, 0); // 4 people

      days.push({
        id: i + 1,
        tripPlanId: trip.id,
        dayNumber: i + 1,
        date: date.toISOString(),
        title: i === 0 ? 'Arrival Day' : i === trip.durationDays - 1 ? 'Departure Day' : `Day ${i + 1} Exploration`,
        activities: dayActivities,
        totalCostUsd: totalCost
      });
    }

    return days;
  }

  private getMockTrip(id: number): TripPlan {
    return {
      id, userId: 1, familyId: 1, familyName: 'The Smith Family',
      destinationId: 1, destinationName: 'Barcelona', destinationCountry: 'Spain',
      destinationImageUrl: 'https://images.unsplash.com/photo-1539037116277-4db20889f2d4?w=600&q=80',
      title: 'Summer in Barcelona', startDate: '2026-07-15', endDate: '2026-07-22',
      durationDays: 7, status: 'Planned' as any, totalBudgetUsd: 2800,
      createdAt: new Date().toISOString()
    };
  }
}
