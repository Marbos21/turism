import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TripPlanService } from '../../core/services/trip-plan.service';
import { FamilyService } from '../../core/services/family.service';
import { DestinationService } from '../../core/services/destination.service';
import { TripPlan, TripStatus } from '../../core/models/trip-plan.model';
import { Family } from '../../core/models/family.model';
import { Destination, ClimateType } from '../../core/models/destination.model';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-trip-planner',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule, DateFormatPipe],
  templateUrl: './trip-planner.component.html',
  styleUrls: ['./trip-planner.component.scss']
})
export class TripPlannerComponent implements OnInit {
  private fb                 = inject(FormBuilder);
  private tripPlanService    = inject(TripPlanService);
  private familyService      = inject(FamilyService);
  private destinationService = inject(DestinationService);
  private router             = inject(Router);
  private route              = inject(ActivatedRoute);

  trips        = signal<TripPlan[]>([]);
  families     = signal<Family[]>([]);
  destinations = signal<Destination[]>([]);
  isLoading    = signal(true);
  isSaving     = signal(false);
  showForm     = signal(false);
  errorMessage = signal('');
  successMessage=signal('');

  form!: FormGroup;
  readonly TripStatus = TripStatus;

  private destinationsLoaded = false;
  private familiesLoaded     = false;
  private preselectApplied   = false;

  ngOnInit(): void {
    this.initForm();
    this.loadData();
  }

  private tryApplyPreselects(): void {
    if (this.preselectApplied) return;
    if (!this.destinationsLoaded || !this.familiesLoaded) return;
    this.preselectApplied = true;

    const destIdParam = this.route.snapshot.queryParams['destinationId'];
    const destination = destIdParam
      ? this.destinations().find(d => String(d.id) === String(destIdParam))
      : undefined;
    const firstFamily = this.families()[0];

    const patch: Record<string, unknown> = {};
    if (firstFamily) patch['familyId'] = firstFamily.id;
    if (destination) {
      patch['destinationId'] = destination.id;
      if (!this.form.get('title')?.value) {
        patch['title'] = `Trip to ${destination.name}`;
      }
      this.showForm.set(true);
    }
    this.form.patchValue(patch);
    this.suggestBudget();
  }

  private initForm(): void {
    const today = new Date();
    const nextMonth = new Date(today);
    nextMonth.setMonth(today.getMonth() + 1);

    this.form = this.fb.group({
      title:           ['', [Validators.required, Validators.minLength(3)]],
      familyId:        [null, Validators.required],
      destinationId:   [null, Validators.required],
      startDate:       [this.formatDate(nextMonth), Validators.required],
      endDate:         [this.formatDate(new Date(nextMonth.getTime() + 7 * 24 * 60 * 60 * 1000)), Validators.required],
      estimatedBudget: [1000, [Validators.required, Validators.min(1)]],
      notes:           ['']
    });

    this.form.get('destinationId')?.valueChanges.subscribe(() => this.suggestBudget());
    this.form.get('startDate')?.valueChanges.subscribe(() => this.suggestBudget());
    this.form.get('endDate')?.valueChanges.subscribe(() => this.suggestBudget());
  }

  private suggestBudget(): void {
    const destId = this.form.get('destinationId')?.value;
    if (!destId) return;
    const dest = this.destinations().find(d => String(d.id) === String(destId));
    const days = this.getDurationDays();
    if (!dest || days <= 0) return;
    const suggested = Math.max(1, Math.round((dest.averageCostPerDayUsd || 0) * days));
    if (suggested > 0) {
      this.form.get('estimatedBudget')?.setValue(suggested, { emitEvent: false });
    }
  }

  private loadData(): void {
    this.destinationService.getDestinations(undefined, 1, 50).subscribe({
      next: (r) => {
        this.destinations.set(r.items);
        this.destinationsLoaded = true;
        this.tryApplyPreselects();
      },
      error: () => {
        this.destinations.set([]);
        this.destinationsLoaded = true;
        this.tryApplyPreselects();
      }
    });

    this.familyService.getMyFamilies().subscribe({
      next: (f) => {
        this.families.set(f);
        this.familiesLoaded = true;
        this.tryApplyPreselects();

        const firstFamilyId = f[0]?.id;
        if (!firstFamilyId) {
          this.trips.set([]);
          this.isLoading.set(false);
          return;
        }
        this.tripPlanService.getMyTrips(firstFamilyId).subscribe({
          next: (t) => {
            this.trips.set(t);
            this.isLoading.set(false);
          },
          error: () => {
            this.trips.set([]);
            this.isLoading.set(false);
          }
        });
      },
      error: () => {
        this.familiesLoaded = true;
        this.tryApplyPreselects();
        this.trips.set([]);
        this.isLoading.set(false);
      }
    });
  }

  saveTrip(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.tripPlanService.createTrip(this.form.value).subscribe({
      next: (trip) => {
        this.trips.update(t => [trip, ...t]);
        this.isSaving.set(false);
        this.showForm.set(false);
        this.form.reset();
        this.initForm();
        this.showSuccess('Trip plan created successfully!');
      },
      error: () => {
        this.isSaving.set(false);
        this.errorMessage.set('Failed to create trip. Please try again.');
      }
    });
  }

  generateItinerary(trip: TripPlan): void {
    this.router.navigate(['/itinerary', trip.id]);
  }

  viewBudget(trip: TripPlan): void {
    this.router.navigate(['/budget', trip.id]);
  }

  deleteTrip(trip: TripPlan): void {
    if (!confirm(`Delete "${trip.title}"?`)) return;
    this.tripPlanService.deleteTrip(trip.id).subscribe({
      next: () => this.trips.update(t => t.filter(x => x.id !== trip.id)),
      error: () => this.errorMessage.set('Failed to delete trip.')
    });
  }

  getStatusClass(status: TripStatus): string {
    const map: Record<string, string> = {
      Draft: 'badge-secondary', Planned: 'badge-info',
      Active: 'badge-success', Completed: 'badge-primary', Cancelled: 'badge-danger'
    };
    return map[status] || 'badge-secondary';
  }

  getDurationDays(): number {
    const start = this.form.get('startDate')?.value;
    const end   = this.form.get('endDate')?.value;
    if (!start || !end) return 0;
    const diff = new Date(end).getTime() - new Date(start).getTime();
    return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  private showSuccess(msg: string): void {
    this.successMessage.set(msg);
    setTimeout(() => this.successMessage.set(''), 4000);
  }

  private getMockTrips(): TripPlan[] {
    return [
      {
        id: 1, userId: 1, familyId: 1, familyName: 'The Smith Family',
        destinationId: 1, destinationName: 'Barcelona', destinationCountry: 'Spain',
        destinationImageUrl: 'https://images.unsplash.com/photo-1539037116277-4db20889f2d4?w=400&q=80',
        title: 'Summer in Barcelona', startDate: '2026-07-15', endDate: '2026-07-22',
        durationDays: 7, status: TripStatus.Planned, totalBudgetUsd: 2800,
        notes: 'Focus on Gaudí architecture and beach time.', createdAt: new Date().toISOString()
      },
      {
        id: 2, userId: 1, familyId: 1, familyName: 'The Smith Family',
        destinationId: 2, destinationName: 'Bali', destinationCountry: 'Indonesia',
        destinationImageUrl: 'https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=400&q=80',
        title: 'Bali Family Retreat', startDate: '2026-12-20', endDate: '2026-12-30',
        durationDays: 10, status: TripStatus.Draft, totalBudgetUsd: 3500,
        createdAt: new Date().toISOString()
      }
    ];
  }

  private getMockDestinations(): Destination[] {
    return [
      { id: 1, name: 'Barcelona', country: 'Spain', region: 'Catalonia', description: '', imageUrl: '', latitude: 0, longitude: 0, childFriendlyScore: 8.5, averageCostPerDayUsd: 120, climate: ClimateType.Mediterranean, bestMonths: [], languages: [], currency: 'EUR', safetyRating: 4, tags: [], highlights: [], weatherSummary: { avgTempCelsius: 20, rainyDaysPerMonth: 5, sunHoursPerDay: 8, humidity: 65 }, createdAt: '' },
      { id: 2, name: 'Bali', country: 'Indonesia', region: 'Bali', description: '', imageUrl: '', latitude: 0, longitude: 0, childFriendlyScore: 8.8, averageCostPerDayUsd: 75, climate: ClimateType.Tropical, bestMonths: [], languages: [], currency: 'IDR', safetyRating: 4, tags: [], highlights: [], weatherSummary: { avgTempCelsius: 28, rainyDaysPerMonth: 8, sunHoursPerDay: 9, humidity: 80 }, createdAt: '' },
      { id: 3, name: 'Tokyo', country: 'Japan', region: 'Kanto', description: '', imageUrl: '', latitude: 0, longitude: 0, childFriendlyScore: 9.2, averageCostPerDayUsd: 200, climate: ClimateType.Temperate, bestMonths: [], languages: [], currency: 'JPY', safetyRating: 5, tags: [], highlights: [], weatherSummary: { avgTempCelsius: 16, rainyDaysPerMonth: 10, sunHoursPerDay: 6, humidity: 70 }, createdAt: '' }
    ];
  }
}
