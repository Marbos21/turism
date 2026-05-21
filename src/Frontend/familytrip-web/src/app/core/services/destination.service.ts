import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import { Destination, DestinationFilter, PagedResult, ClimateType } from '../models/destination.model';
import { DestinationDetails } from '../models/destination-details.model';

interface BackendDestination {
  id: string;
  name: string;
  country: string;
  latitude: number;
  longitude: number;
  description: string;
  category: string;
  childFriendlyScore: number;
  avgCostPerDay: number;
  climate: number;
  imageUrl: string;
  region?: string | null;
  capital?: string | null;
  languages?: string[] | null;
  currency?: string | null;
  bestMonths?: number[] | null;
}

@Injectable({ providedIn: 'root' })
export class DestinationService {
  constructor(private api: ApiService) {}

  getDestinations(filter?: DestinationFilter, page = 1, pageSize = 12): Observable<PagedResult<Destination>> {
    const params: Record<string, string | number | boolean> = {};
    if (filter?.search)       params['searchTerm']            = filter.search;
    if (filter?.country)      params['country']               = filter.country;
    if (filter?.climate && this.climateStringToBackendName(filter.climate)) {
      params['climate'] = this.climateStringToBackendName(filter.climate)!;
    }
    if (filter?.minChildScore)  params['minChildFriendlyScore'] = filter.minChildScore;
    if (filter?.maxCostPerDay)  params['maxCostPerDay']         = filter.maxCostPerDay;

    return this.api.get<BackendDestination[]>('/destinations', params).pipe(
      map(list => {
        const items = (list ?? []).map(d => this.toFeDestination(d));
        const totalCount = items.length;
        const start = (page - 1) * pageSize;
        return {
          items: items.slice(start, start + pageSize),
          totalCount,
          page,
          pageSize,
          totalPages: Math.max(1, Math.ceil(totalCount / pageSize))
        };
      })
    );
  }

  getDestinationById(id: number | string): Observable<Destination> {
    return this.api.get<BackendDestination>(`/destinations/${id}`).pipe(
      map(d => this.toFeDestination(d))
    );
  }

  getDestinationDetails(id: number | string): Observable<DestinationDetails> {
    return this.api
      .get<{
        destination: BackendDestination;
        weather: DestinationDetails['weather'];
        attractions: DestinationDetails['attractions'];
        hotels: DestinationDetails['hotels'];
      }>(`/destinations/${id}/details`)
      .pipe(map(res => ({
        destination: this.toFeDestination(res.destination),
        weather: res.weather ?? null,
        attractions: res.attractions ?? [],
        hotels: res.hotels ?? null
      })));
  }

  getFeaturedDestinations(): Observable<Destination[]> {
    return this.api.get<BackendDestination[]>('/destinations/popular').pipe(
      map(list => (list ?? []).map(d => this.toFeDestination(d)))
    );
  }

  searchDestinations(query: string): Observable<Destination[]> {
    return this.api.get<BackendDestination[]>('/destinations', { searchTerm: query }).pipe(
      map(list => (list ?? []).map(d => this.toFeDestination(d)))
    );
  }

  private toFeDestination(be: BackendDestination): Destination {
    return {
      id: be.id as unknown as number,
      name: be.name ?? '',
      country: be.country ?? '',
      region: be.region ?? '',
      description: be.description ?? '',
      imageUrl: be.imageUrl ?? '',
      latitude: be.latitude,
      longitude: be.longitude,
      childFriendlyScore: be.childFriendlyScore,
      averageCostPerDayUsd: be.avgCostPerDay,
      climate: this.climateIntToFeEnum(be.climate),
      bestMonths: be.bestMonths ?? [],
      languages: be.languages ?? [],
      currency: be.currency ?? '',
      safetyRating: 0,
      tags: be.category ? [be.category] : [],
      highlights: [],
      weatherSummary: { avgTempCelsius: 0, rainyDaysPerMonth: 0, sunHoursPerDay: 0, humidity: 0 },
      createdAt: new Date(0).toISOString()
    };
  }

  private climateIntToFeEnum(value: number): ClimateType {
    switch (value) {
      case 1: return ClimateType.Tropical;
      case 2: return ClimateType.Mediterranean;
      case 3: return ClimateType.Continental;
      case 4: return ClimateType.Polar;
      default: return ClimateType.Temperate;
    }
  }

  private climateStringToBackendName(climate: ClimateType): string | null {
    switch (climate) {
      case ClimateType.Tropical:      return 'Tropical';
      case ClimateType.Mediterranean: return 'Mediterranean';
      case ClimateType.Continental:   return 'Continental';
      case ClimateType.Polar:         return 'Arctic';
      default: return null;
    }
  }
}
