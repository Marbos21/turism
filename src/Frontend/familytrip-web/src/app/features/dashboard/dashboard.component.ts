import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TripPlanService } from '../../core/services/trip-plan.service';
import { FamilyService } from '../../core/services/family.service';
import { TripPlan, TripStatus } from '../../core/models/trip-plan.model';
import { Family } from '../../core/models/family.model';
import { DateFormatPipe } from '../../shared/pipes/date-format.pipe';

interface QuickStat {
  label: string;
  value: number | string;
  icon: string;
  color: string;
  route: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, DateFormatPipe],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  readonly authService     = inject(AuthService);
  private tripPlanService  = inject(TripPlanService);
  private familyService    = inject(FamilyService);

  trips    = signal<TripPlan[]>([]);
  families = signal<Family[]>([]);
  isLoading= signal(true);

  quickStats: QuickStat[] = [];
  upcomingTrips: TripPlan[] = [];

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.familyService.getMyFamilies().subscribe({
      next: (families) => {
        this.families.set(families);
        const firstFamilyId = families[0]?.id;

        if (!firstFamilyId) {
          this.buildStats();
          this.isLoading.set(false);
          return;
        }

        this.tripPlanService.getMyTrips(firstFamilyId).subscribe({
          next: (trips) => {
            this.trips.set(trips);
            this.upcomingTrips = trips
              .filter(t => t.status !== TripStatus.Completed && t.status !== TripStatus.Cancelled)
              .slice(0, 3);
            this.buildStats();
            this.isLoading.set(false);
          },
          error: () => {
            this.buildStats();
            this.isLoading.set(false);
          }
        });
      },
      error: () => {
        this.buildStats();
        this.isLoading.set(false);
      }
    });
  }

  private buildStats(): void {
    const memberCount = this.families()[0]?.members?.length ?? 0;
    this.quickStats = [
      {
        label: 'My Families',
        value: this.families().length,
        icon: 'family_restroom',
        color: '#00bcd4',
        route: '/family'
      },
      {
        label: 'Family Members',
        value: memberCount,
        icon: 'group',
        color: '#9c27b0',
        route: '/family'
      },
      {
        label: 'Trip Plans',
        value: this.trips().length,
        icon: 'map',
        color: '#009688',
        route: '/trip-planner'
      },
      {
        label: 'Upcoming Trips',
        value: this.upcomingTrips.length,
        icon: 'flight_takeoff',
        color: '#2196f3',
        route: '/trip-planner'
      }
    ];
  }

  get greeting(): string {
    const hour = new Date().getHours();
    if (hour < 12) return 'Good morning';
    if (hour < 18) return 'Good afternoon';
    return 'Good evening';
  }

  get userName(): string {
    return this.authService.currentUser()?.fullName?.split(' ')[0] || 'Traveler';
  }

  getTripStatusClass(status: TripStatus): string {
    const map: Record<string, string> = {
      Draft:     'badge-secondary',
      Planned:   'badge-info',
      Active:    'badge-success',
      Completed: 'badge-primary',
      Cancelled: 'badge-danger'
    };
    return map[status] || 'badge-secondary';
  }
}
