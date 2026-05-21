import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  Recommendation, GenerateRecommendationsRequest, GenerateRecommendationsResult, AgentMeta
} from '../models/recommendation.model';

interface BackendDestination {
  id: string;
  name: string;
  country: string;
  imageUrl: string;
  avgCostPerDay: number;
  climate: number;
}

interface BackendRecommendation {
  id: string;
  familyId: string;
  destinationId: string;
  destination: BackendDestination | null;
  score: number;
  reasoning: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class RecommendationService {
  constructor(private api: ApiService) {}

  getRecommendations(familyId: number | string): Observable<Recommendation[]> {
    return this.api.get<BackendRecommendation[]>(`/recommendations/family/${familyId}`).pipe(
      map(list => (list ?? []).map(r => this.toFeRecommendation(r)))
    );
  }

  generateRecommendations(request: GenerateRecommendationsRequest): Observable<GenerateRecommendationsResult> {
    return this.api
      .post<{ recommendations: BackendRecommendation[]; agentMeta: AgentMeta | null }>(
        '/recommendations/generate', { familyId: request.familyId })
      .pipe(map(res => ({
        recommendations: (res?.recommendations ?? []).map(r => this.toFeRecommendation(r)),
        agentMeta: res?.agentMeta ?? null
      })));
  }

  dismissRecommendation(_id: number | string): Observable<void> {
    return of(void 0);
  }

  private toFeRecommendation(be: BackendRecommendation): Recommendation {
    const dest = be.destination;
    return {
      id: be.id as unknown as number,
      familyId: be.familyId as unknown as number,
      destinationId: be.destinationId as unknown as number,
      destinationName: dest?.name ?? '',
      destinationCountry: dest?.country ?? '',
      destinationImageUrl: dest?.imageUrl ?? '',
      compatibilityScore: Math.round(be.score ?? 0),
      reasoning: be.reasoning ?? '',
      highlights: [],
      estimatedBudgetUsd: dest?.avgCostPerDay ? Math.round(dest.avgCostPerDay * 7) : 0,
      bestTravelMonths: this.bestMonthsForClimate(dest?.climate),
      warnings: [],
      generatedAt: be.createdAt
    };
  }

  private bestMonthsForClimate(climate: number | undefined): number[] {
    // Climate enum (BE): Tropical=1, Mediterranean=2, Continental=3, Arctic=4
    switch (climate) {
      case 1: return [11, 12, 1, 2, 3, 4];
      case 2: return [4, 5, 6, 9, 10];
      case 3: return [5, 6, 7, 8, 9];
      case 4: return [6, 7, 8];
      default: return [];
    }
  }
}
