import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { DestinationService } from '../../../core/services/destination.service';
import { Destination, ClimateType } from '../../../core/models/destination.model';
import { DestinationDetails } from '../../../core/models/destination-details.model';
import { MapComponent } from '../../../shared/components/map/map.component';

@Component({
  selector: 'app-destination-detail',
  standalone: true,
  imports: [CommonModule, RouterModule, MapComponent],
  templateUrl: './destination-detail.component.html',
  styleUrls: ['./destination-detail.component.scss']
})
export class DestinationDetailComponent implements OnInit {
  private route              = inject(ActivatedRoute);
  private destinationService = inject(DestinationService);

  destination = signal<Destination | null>(null);
  details     = signal<DestinationDetails | null>(null);
  isLoading   = signal(true);
  activeTab   = signal<'overview' | 'weather' | 'tips'>('overview');

  readonly monthNames = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadDestination(id);
  }

  private loadDestination(id: string): void {
    this.destinationService.getDestinationDetails(id).subscribe({
      next: (d) => {
        this.destination.set(d.destination);
        this.details.set(d);
        this.isLoading.set(false);
      },
      error: () => {
        this.destination.set(null);
        this.details.set(null);
        this.isLoading.set(false);
      }
    });
  }

  getScoreColor(score: number): string {
    if (score >= 8) return '#4caf50';
    if (score >= 6) return '#ff9800';
    return '#f44336';
  }

  getScoreWidth(score: number): string {
    return (score * 10) + '%';
  }

  isBestMonth(monthIndex: number): boolean {
    return this.destination()?.bestMonths.includes(monthIndex + 1) ?? false;
  }

  getSafetyStars(rating: number): boolean[] {
    return Array.from({ length: 5 }, (_, i) => i < rating);
  }

  private getMockDestination(id: number): Destination {
    const destinations: Record<number, Destination> = {
      1: {
        id: 1, name: 'Barcelona', country: 'Spain', region: 'Catalonia',
        description: 'Barcelona is a vibrant city on the northeastern coast of Spain. It\'s known for its stunning modernist architecture by Antoni Gaudí, including the iconic Sagrada Família basilica and the Park Güell. The city offers beautiful beaches, world-class cuisine, a thriving arts scene, and a mix of Gothic and modern neighborhoods. Perfect for families with children of all ages, offering excellent public transport, safe streets, and a warm Mediterranean climate.',
        imageUrl: 'https://images.unsplash.com/photo-1539037116277-4db20889f2d4?w=1200&q=80',
        latitude: 41.3851, longitude: 2.1734,
        childFriendlyScore: 8.5, averageCostPerDayUsd: 120,
        climate: ClimateType.Mediterranean, bestMonths: [4,5,6,9,10],
        languages: ['Spanish', 'Catalan'], currency: 'EUR',
        safetyRating: 4, tags: ['Beach', 'Culture', 'Architecture', 'Food', 'Family'],
        highlights: ['Sagrada Família', 'Park Güell', 'Barcelona Zoo', 'Tibidabo Amusement Park', 'Barceloneta Beach'],
        weatherSummary: { avgTempCelsius: 20, rainyDaysPerMonth: 5, sunHoursPerDay: 8, humidity: 65 },
        createdAt: new Date().toISOString()
      }
    };
    return destinations[id] || destinations[1];
  }
}
