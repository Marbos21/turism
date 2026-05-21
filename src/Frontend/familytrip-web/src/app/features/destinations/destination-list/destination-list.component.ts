import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { DestinationService } from '../../../core/services/destination.service';
import { Destination, ClimateType, DestinationFilter } from '../../../core/models/destination.model';

@Component({
  selector: 'app-destination-list',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './destination-list.component.html',
  styleUrls: ['./destination-list.component.scss']
})
export class DestinationListComponent implements OnInit {
  private destinationService = inject(DestinationService);

  destinations   = signal<Destination[]>([]);
  isLoading      = signal(true);
  totalCount     = signal(0);
  currentPage    = signal(1);
  totalPages     = signal(1);
  readonly pageSize = 12;

  searchControl  = new FormControl('');
  climateFilter  = signal<ClimateType | ''>('');
  minChildScore  = signal(0);
  maxCost        = signal(0);
  selectedCountry= signal('');

  readonly climateOptions = Object.values(ClimateType);

  ngOnInit(): void {
    this.loadDestinations();

    this.searchControl.valueChanges.pipe(
      debounceTime(400),
      distinctUntilChanged()
    ).subscribe(() => {
      this.currentPage.set(1);
      this.loadDestinations();
    });
  }

  loadDestinations(): void {
    this.isLoading.set(true);

    const filter: DestinationFilter = {
      search:       this.searchControl.value || undefined,
      climate:      this.climateFilter() || undefined,
      minChildScore:this.minChildScore() || undefined,
      maxCostPerDay:this.maxCost() || undefined,
      country:      this.selectedCountry() || undefined
    };

    this.destinationService.getDestinations(filter, this.currentPage(), this.pageSize).subscribe({
      next: (result) => {
        this.destinations.set(result.items);
        this.totalCount.set(result.totalCount);
        this.totalPages.set(result.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        this.destinations.set(this.getMockDestinations());
        this.isLoading.set(false);
      }
    });
  }

  applyFilters(): void {
    this.currentPage.set(1);
    this.loadDestinations();
  }

  clearFilters(): void {
    this.searchControl.setValue('');
    this.climateFilter.set('');
    this.minChildScore.set(0);
    this.maxCost.set(0);
    this.selectedCountry.set('');
    this.currentPage.set(1);
    this.loadDestinations();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage.set(page);
    this.loadDestinations();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  getClimateClass(climate: ClimateType): string {
    const map: Record<string, string> = {
      Tropical:      'badge-success',
      Subtropical:   'badge-success',
      Mediterranean: 'badge-warning',
      Temperate:     'badge-info',
      Continental:   'badge-info',
      Arid:          'badge-warning',
      Polar:         'badge-primary',
      Mountain:      'badge-secondary'
    };
    return map[climate] || 'badge-secondary';
  }

  getScoreColor(score: number): string {
    if (score >= 8) return '#4caf50';
    if (score >= 6) return '#ff9800';
    return '#f44336';
  }

  getScoreLabel(score: number): string {
    if (score >= 8) return 'Excellent';
    if (score >= 6) return 'Good';
    if (score >= 4) return 'Fair';
    return 'Limited';
  }

  get pageNumbers(): number[] {
    const pages: number[] = [];
    const total = this.totalPages();
    const current = this.currentPage();
    for (let i = Math.max(1, current - 2); i <= Math.min(total, current + 2); i++) {
      pages.push(i);
    }
    return pages;
  }

  private getMockDestinations(): Destination[] {
    return [
      {
        id: 1, name: 'Barcelona', country: 'Spain', region: 'Catalonia',
        description: 'A vibrant city with stunning architecture, beautiful beaches, and world-class cuisine.',
        imageUrl: 'https://images.unsplash.com/photo-1539037116277-4db20889f2d4?w=600&q=80',
        latitude: 41.3851, longitude: 2.1734,
        childFriendlyScore: 8.5, averageCostPerDayUsd: 120,
        climate: ClimateType.Mediterranean, bestMonths: [4,5,6,9,10],
        languages: ['Spanish', 'Catalan'], currency: 'EUR',
        safetyRating: 4, tags: ['Beach', 'Culture', 'Architecture', 'Food'],
        highlights: ['Sagrada Família', 'Park Güell', 'Las Ramblas', 'Camp Nou'],
        weatherSummary: { avgTempCelsius: 20, rainyDaysPerMonth: 5, sunHoursPerDay: 8, humidity: 65 },
        createdAt: new Date().toISOString()
      },
      {
        id: 2, name: 'Bali', country: 'Indonesia', region: 'Bali Province',
        description: 'Tropical paradise with lush rice terraces, ancient temples, and stunning beaches.',
        imageUrl: 'https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=80',
        latitude: -8.3405, longitude: 115.0920,
        childFriendlyScore: 8.8, averageCostPerDayUsd: 75,
        climate: ClimateType.Tropical, bestMonths: [4,5,6,7,8,9],
        languages: ['Balinese', 'Indonesian'], currency: 'IDR',
        safetyRating: 4, tags: ['Beach', 'Culture', 'Nature', 'Temples'],
        highlights: ['Ubud Monkey Forest', 'Tanah Lot Temple', 'Seminyak Beach', 'Mount Batur'],
        weatherSummary: { avgTempCelsius: 28, rainyDaysPerMonth: 8, sunHoursPerDay: 9, humidity: 80 },
        createdAt: new Date().toISOString()
      },
      {
        id: 3, name: 'Tokyo', country: 'Japan', region: 'Kanto',
        description: 'A fascinating blend of ultra-modern and traditional, with family-friendly attractions.',
        imageUrl: 'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=600&q=80',
        latitude: 35.6762, longitude: 139.6503,
        childFriendlyScore: 9.2, averageCostPerDayUsd: 200,
        climate: ClimateType.Temperate, bestMonths: [3,4,5,10,11],
        languages: ['Japanese'], currency: 'JPY',
        safetyRating: 5, tags: ['Culture', 'Technology', 'Food', 'Theme Parks'],
        highlights: ['Disneyland', 'TeamLab Museums', 'Shibuya Crossing', 'Mount Fuji'],
        weatherSummary: { avgTempCelsius: 16, rainyDaysPerMonth: 10, sunHoursPerDay: 6, humidity: 70 },
        createdAt: new Date().toISOString()
      },
      {
        id: 4, name: 'Lisbon', country: 'Portugal', region: 'Estremadura',
        description: 'Charming coastal city with colorful neighborhoods, trams, and delicious pastéis.',
        imageUrl: 'https://images.unsplash.com/photo-1555881400-74d7acaacd8b?w=600&q=80',
        latitude: 38.7223, longitude: -9.1393,
        childFriendlyScore: 8.0, averageCostPerDayUsd: 100,
        climate: ClimateType.Mediterranean, bestMonths: [4,5,6,7,8,9],
        languages: ['Portuguese'], currency: 'EUR',
        safetyRating: 5, tags: ['Culture', 'Architecture', 'Beach', 'History'],
        highlights: ['Belém Tower', 'Sintra Palaces', 'Oceanarium', 'Alfama District'],
        weatherSummary: { avgTempCelsius: 18, rainyDaysPerMonth: 6, sunHoursPerDay: 8, humidity: 60 },
        createdAt: new Date().toISOString()
      },
      {
        id: 5, name: 'Cancún', country: 'Mexico', region: 'Quintana Roo',
        description: 'World-class beaches, ancient Mayan ruins, and all-inclusive family resorts.',
        imageUrl: 'https://images.unsplash.com/photo-1510097467424-192d713fd8b2?w=600&q=80',
        latitude: 21.1619, longitude: -86.8515,
        childFriendlyScore: 9.0, averageCostPerDayUsd: 150,
        climate: ClimateType.Tropical, bestMonths: [11,12,1,2,3,4],
        languages: ['Spanish'], currency: 'MXN',
        safetyRating: 3, tags: ['Beach', 'Adventure', 'History', 'All-Inclusive'],
        highlights: ['Chichen Itza', 'Xcaret Park', 'Tulum Ruins', 'Isla Mujeres'],
        weatherSummary: { avgTempCelsius: 27, rainyDaysPerMonth: 7, sunHoursPerDay: 9, humidity: 75 },
        createdAt: new Date().toISOString()
      },
      {
        id: 6, name: 'Prague', country: 'Czech Republic', region: 'Bohemia',
        description: 'Fairy-tale architecture, rich history, and a magical atmosphere year-round.',
        imageUrl: 'https://images.unsplash.com/photo-1541849546-216549ae216d?w=600&q=80',
        latitude: 50.0755, longitude: 14.4378,
        childFriendlyScore: 7.5, averageCostPerDayUsd: 90,
        climate: ClimateType.Continental, bestMonths: [4,5,6,7,8,9],
        languages: ['Czech'], currency: 'CZK',
        safetyRating: 5, tags: ['History', 'Architecture', 'Culture', 'City'],
        highlights: ['Prague Castle', 'Charles Bridge', 'Old Town Square', 'Prague Zoo'],
        weatherSummary: { avgTempCelsius: 12, rainyDaysPerMonth: 8, sunHoursPerDay: 5, humidity: 70 },
        createdAt: new Date().toISOString()
      }
    ];
  }
}
