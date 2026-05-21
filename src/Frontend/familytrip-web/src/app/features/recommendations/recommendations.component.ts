import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ReactiveFormsModule, FormControl } from '@angular/forms';
import { RecommendationService } from '../../core/services/recommendation.service';
import { DestinationService } from '../../core/services/destination.service';
import { FamilyService } from '../../core/services/family.service';
import { Recommendation, AgentMeta } from '../../core/models/recommendation.model';
import { DestinationDetails } from '../../core/models/destination-details.model';
import { Family } from '../../core/models/family.model';

@Component({
  selector: 'app-recommendations',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  templateUrl: './recommendations.component.html',
  styleUrls: ['./recommendations.component.scss']
})
export class RecommendationsComponent implements OnInit {
  private recommendationService = inject(RecommendationService);
  private destinationService    = inject(DestinationService);
  private familyService         = inject(FamilyService);

  families        = signal<Family[]>([]);
  recommendations = signal<Recommendation[]>([]);
  isLoading       = signal(true);
  isGenerating    = signal(false);
  selectedFamily  = signal<number | null>(null);
  errorMessage    = signal('');

  agentMeta       = signal<AgentMeta | null>(null);
  showTracePanel  = signal(false);
  destinationDetails = signal<Record<string, DestinationDetails>>({});

  familyControl = new FormControl<number | null>(null);

  readonly monthNames = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];

  ngOnInit(): void {
    this.familyService.getMyFamilies().subscribe({
      next: (f) => {
        this.families.set(f);
        if (f.length) {
          this.selectedFamily.set(f[0].id);
          this.familyControl.setValue(f[0].id);
          this.loadRecommendations(f[0].id);
        } else {
          this.isLoading.set(false);
          this.recommendations.set(this.getMockRecommendations());
        }
      },
      error: () => {
        this.isLoading.set(false);
        this.recommendations.set(this.getMockRecommendations());
      }
    });
  }

  private loadRecommendations(familyId: number): void {
    this.isLoading.set(true);
    this.agentMeta.set(null);
    this.destinationDetails.set({});
    this.recommendationService.getRecommendations(familyId).subscribe({
      next: (recs) => {
        this.recommendations.set(recs);
        this.isLoading.set(false);
        this.loadDetailsForAll(recs);
      },
      error: () => {
        this.recommendations.set(this.getMockRecommendations());
        this.isLoading.set(false);
      }
    });
  }

  private loadDetailsForAll(recs: Recommendation[]): void {
    for (const rec of recs) {
      const destId = String(rec.destinationId);
      if (!destId || destId === 'undefined' || destId === 'null') continue;
      this.destinationService.getDestinationDetails(destId).subscribe({
        next: (details) => {
          this.destinationDetails.update(m => ({ ...m, [destId]: details }));
        },
        error: () => {}
      });
    }
  }

  detailsFor(rec: Recommendation): DestinationDetails | undefined {
    return this.destinationDetails()[String(rec.destinationId)];
  }

  hasAnyDataFor(rec: Recommendation): boolean {
    const d = this.detailsFor(rec);
    return !!(d?.weather || d?.attractions?.length || d?.hotels?.hotelsFound);
  }

  topAttractionNames(rec: Recommendation): string {
    const d = this.detailsFor(rec);
    if (!d?.attractions?.length) return '';
    return 'Top attractions: ' + d.attractions.slice(0, 3).map(a => a.name).join(', ');
  }

  toggleTrace(): void {
    this.showTracePanel.update(v => !v);
  }

  parseToolOutput(output: string): string {
    try {
      const obj = JSON.parse(output);
      if (obj.available === false) return `not available (${obj.reason || 'no data'})`;
      if (obj.error) return `error: ${obj.error}`;
      if (Array.isArray(obj.places)) return `${obj.places.length} places: ${obj.places.slice(0,3).map((p: any) => p.Name || p.name).join(', ')}${obj.places.length > 3 ? '...' : ''}`;
      if (obj.hotelsFound !== undefined) return `${obj.hotelsFound} hotels nearby`;
      if (obj.avgTempC !== undefined) return `${obj.avgTempC}°C avg, ${obj.rainyDays} rainy days, ${obj.summary}`;
      if (obj.accepted) return 'rankings accepted';
      return JSON.stringify(obj).slice(0, 100);
    } catch {
      return output.slice(0, 100);
    }
  }

  toolIcon(name: string): string {
    switch (name) {
      case 'get_destination_weather': return 'wb_sunny';
      case 'get_points_of_interest':  return 'place';
      case 'get_hotel_availability':  return 'hotel';
      case 'submit_recommendations':  return 'check_circle';
      default: return 'build';
    }
  }

  onFamilyChange(): void {
    const id = this.familyControl.value;
    if (id) {
      this.selectedFamily.set(id);
      this.loadRecommendations(id);
    }
  }

  generateRecommendations(): void {
    const familyId = this.selectedFamily();
    if (!familyId) return;

    this.isGenerating.set(true);
    this.agentMeta.set(null);
    this.destinationDetails.set({});
    this.recommendationService.generateRecommendations({ familyId }).subscribe({
      next: (result) => {
        this.recommendations.set(result.recommendations);
        this.agentMeta.set(result.agentMeta);
        this.isGenerating.set(false);
        this.loadDetailsForAll(result.recommendations);
      },
      error: () => {
        this.recommendations.set(this.getMockRecommendations());
        this.isGenerating.set(false);
      }
    });
  }

  dismissRecommendation(rec: Recommendation): void {
    this.recommendationService.dismissRecommendation(rec.id).subscribe({
      next: () => this.recommendations.update(r => r.filter(x => x.id !== rec.id)),
      error: () => this.recommendations.update(r => r.filter(x => x.id !== rec.id))
    });
  }

  getScoreColor(score: number): string {
    if (score >= 80) return '#4caf50';
    if (score >= 60) return '#ff9800';
    return '#f44336';
  }

  getScoreLabel(score: number): string {
    if (score >= 90) return 'Perfect Match';
    if (score >= 80) return 'Excellent';
    if (score >= 70) return 'Very Good';
    if (score >= 60) return 'Good';
    return 'Fair';
  }

  getBestMonthNames(months: number[]): string {
    return months.map(m => this.monthNames[m - 1]).join(', ');
  }

  private getMockRecommendations(): Recommendation[] {
    return [
      {
        id: 1, familyId: 1,
        destinationId: 1, destinationName: 'Barcelona', destinationCountry: 'Spain',
        destinationImageUrl: 'https://images.unsplash.com/photo-1539037116277-4db20889f2d4?w=600&q=80',
        compatibilityScore: 94,
        reasoning: 'Barcelona is an exceptional match for your family profile. The Mediterranean climate aligns with your preferences, and the city offers world-class cultural attractions that appeal to both adults and children. The availability of kid-friendly beaches, interactive museums like the Barcelona Aquarium and CosmoCaixa, and excellent family restaurants make it ideal for your travel style. The moderate cost aligns well with your stated budget level.',
        highlights: [
          { category: 'Cultural Fit',      description: 'World-class museums and Gaudí architecture perfect for educational trips',  icon: 'museum' },
          { category: 'Kid-Friendly',       description: 'Beaches, aquarium, and theme parks for all age groups',                     icon: 'child_friendly' },
          { category: 'Climate',            description: 'Warm Mediterranean summers ideal for outdoor activities',                   icon: 'wb_sunny' },
          { category: 'Food & Cuisine',     description: 'Diverse restaurant scene with many child-friendly options',                 icon: 'restaurant' }
        ],
        estimatedBudgetUsd: 2800,
        bestTravelMonths: [4, 5, 6, 9, 10],
        warnings: [],
        generatedAt: new Date().toISOString()
      },
      {
        id: 2, familyId: 1,
        destinationId: 2, destinationName: 'Bali', destinationCountry: 'Indonesia',
        destinationImageUrl: 'https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=600&q=80',
        compatibilityScore: 88,
        reasoning: 'Bali offers a superb blend of cultural richness and natural beauty that matches your family\'s interest in both adventure and relaxation. The tropical climate, affordable family resorts, and iconic rice terraces provide unforgettable experiences. Children will love the monkey forest, water activities, and colorful ceremonies. The cost-effectiveness of Bali makes it outstanding value for families on a moderate budget.',
        highlights: [
          { category: 'Nature',         description: 'Stunning rice terraces, volcanoes, and tropical beaches',         icon: 'landscape' },
          { category: 'Budget Value',   description: 'Exceptional value — family resort stays from $80/night',          icon: 'savings' },
          { category: 'Adventure',      description: 'White water rafting, cycling, and jungle trekking for active families', icon: 'hiking' },
          { category: 'Culture',        description: 'Rich Hindu heritage with family-friendly temple visits and ceremonies', icon: 'temple_hindu' }
        ],
        estimatedBudgetUsd: 2200,
        bestTravelMonths: [4, 5, 6, 7, 8, 9],
        warnings: ['Long-haul flight — consider jet lag for younger children', 'Some areas require appropriate dress for temple visits'],
        generatedAt: new Date().toISOString()
      },
      {
        id: 3, familyId: 1,
        destinationId: 3, destinationName: 'Tokyo', destinationCountry: 'Japan',
        destinationImageUrl: 'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=600&q=80',
        compatibilityScore: 82,
        reasoning: 'Tokyo is consistently ranked as one of the world\'s safest and most child-friendly cities. The combination of futuristic technology, ancient traditions, anime culture, and incredible cuisine makes it fascinating for families with older children and teenagers. Tokyo Disneyland and TeamLab immersive art museums are world-class family attractions. The main consideration is the higher daily costs compared to other destinations.',
        highlights: [
          { category: 'Safety',         description: 'Rated the world\'s safest major city — perfect for families',         icon: 'security' },
          { category: 'Entertainment',  description: 'Tokyo Disneyland, TeamLab Museums, Akihabara — kids will be amazed', icon: 'attractions' },
          { category: 'Transport',      description: 'Excellent public transport with IC cards for the whole family',        icon: 'directions_transit' },
          { category: 'Cuisine',        description: 'Incredible variety from conveyor belt sushi to ramen and curry',       icon: 'ramen_dining' }
        ],
        estimatedBudgetUsd: 5200,
        bestTravelMonths: [3, 4, 5, 10, 11],
        warnings: ['Higher daily costs — budget $200+ per person per day', 'Language barrier — learn basic Japanese phrases'],
        generatedAt: new Date().toISOString()
      },
      {
        id: 4, familyId: 1,
        destinationId: 4, destinationName: 'Lisbon', destinationCountry: 'Portugal',
        destinationImageUrl: 'https://images.unsplash.com/photo-1555881400-74d7acaacd8b?w=600&q=80',
        compatibilityScore: 79,
        reasoning: 'Lisbon is a charming, walkable city that offers an authentic European experience at a very reasonable price point. The famous trams, colorful Alfama district, and nearby Sintra fairy-tale palaces are magical for children. Portugal is one of Europe\'s safest countries and locals are very welcoming to families. The beaches near Lisbon are among Europe\'s finest.',
        highlights: [
          { category: 'History',     description: 'Age-of-discovery heritage with castles, palaces, and maritime museums', icon: 'account_balance' },
          { category: 'Beaches',     description: 'Beautiful Atlantic beaches within 30 minutes of the city center',        icon: 'beach_access' },
          { category: 'Affordability', description: 'One of Western Europe\'s most affordable capitals',                   icon: 'price_check' },
          { category: 'Day Trips',   description: 'Sintra palaces, Cascais, and Setúbal National Park nearby',             icon: 'tour' }
        ],
        estimatedBudgetUsd: 2100,
        bestTravelMonths: [4, 5, 6, 7, 8, 9],
        warnings: [],
        generatedAt: new Date().toISOString()
      }
    ];
  }
}
