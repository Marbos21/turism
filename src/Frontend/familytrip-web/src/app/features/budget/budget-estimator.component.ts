import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { TripPlanService } from '../../core/services/trip-plan.service';
import { TripPlan, BudgetBreakdown } from '../../core/models/trip-plan.model';

interface BudgetCategory {
  label: string;
  amount: number;
  percentage: number;
  icon: string;
  color: string;
}

interface PieSegment {
  label: string;
  d: string;
  color: string;
}

@Component({
  selector: 'app-budget-estimator',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './budget-estimator.component.html',
  styleUrls: ['./budget-estimator.component.scss']
})
export class BudgetEstimatorComponent implements OnInit {
  private route           = inject(ActivatedRoute);
  private tripPlanService = inject(TripPlanService);

  trip      = signal<TripPlan | null>(null);
  budget    = signal<BudgetBreakdown | null>(null);
  isLoading = signal(true);

  categories   = signal<BudgetCategory[]>([]);
  pieSegments: PieSegment[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadBudget(id);
  }

  private loadBudget(id: string): void {
    this.tripPlanService.getTripById(id).subscribe({
      next: (trip) => {
        this.trip.set(trip);
        // The budget is computed client-side from the trip's stored itinerary,
        // so it always reconciles with what's shown on the itinerary page.
        this.tripPlanService.getBudgetEstimate(id).subscribe({
          next: (b) => {
            this.budget.set(b);
            this.buildCategories(b);
            this.isLoading.set(false);
          },
          error: () => {
            this.budget.set(null);
            this.isLoading.set(false);
          }
        });
      },
      error: () => {
        this.trip.set(null);
        this.budget.set(null);
        this.isLoading.set(false);
      }
    });
  }

  private buildCategories(b: BudgetBreakdown): void {
    const total = b.totalUsd || 1; // guard divide-by-zero
    const pct = (amount: number) => Math.round((amount / total) * 100);
    const cats: BudgetCategory[] = [
      { label: 'Accommodation', amount: b.accommodationUsd, percentage: pct(b.accommodationUsd), icon: 'hotel',      color: '#2196f3' },
      { label: 'Transport',     amount: b.transportUsd,     percentage: pct(b.transportUsd),     icon: 'flight',     color: '#ff9800' },
      { label: 'Food',          amount: b.foodUsd,          percentage: pct(b.foodUsd),          icon: 'restaurant', color: '#4caf50' },
      { label: 'Activities',    amount: b.activitiesUsd,    percentage: pct(b.activitiesUsd),    icon: 'attractions',color: '#9c27b0' },
      { label: 'Other',         amount: b.otherUsd,         percentage: pct(b.otherUsd),         icon: 'more_horiz', color: '#607d8b' }
    ].filter(c => c.amount > 0);
    this.categories.set(cats);
    this.buildPieSegments(cats);
  }

  private buildPieSegments(cats: BudgetCategory[]): void {
    let cumulative = 0;
    const cx = 100, cy = 100, r = 80;
    const segments: PieSegment[] = [];

    for (const cat of cats) {
      const startAngle = (cumulative / 100) * 360 - 90;
      const endAngle   = ((cumulative + cat.percentage) / 100) * 360 - 90;
      const x1 = cx + r * Math.cos((startAngle * Math.PI) / 180);
      const y1 = cy + r * Math.sin((startAngle * Math.PI) / 180);
      const x2 = cx + r * Math.cos((endAngle   * Math.PI) / 180);
      const y2 = cy + r * Math.sin((endAngle   * Math.PI) / 180);
      const largeArc = cat.percentage > 50 ? 1 : 0;
      segments.push({
        label: cat.label,
        color: cat.color,
        d: `M ${cx} ${cy} L ${x1.toFixed(2)} ${y1.toFixed(2)} A ${r} ${r} 0 ${largeArc} 1 ${x2.toFixed(2)} ${y2.toFixed(2)} Z`
      });
      cumulative += cat.percentage;
    }
    this.pieSegments = segments;
  }

  getBudgetTip(category: string): string {
    const tips: Record<string, string> = {
      'Accommodation': 'Consider apartment rentals for families — they often offer better value and kitchen access, saving on food costs.',
      'Transport':     'Book flights 3–6 months in advance for best prices. Consider travel with hand luggage only for short trips.',
      'Food':          'Mix restaurant meals with supermarket shopping. Lunch is often cheaper than dinner at restaurants.',
      'Activities':    'Many city museums offer free admission on specific days. Check for family passes and bundle tickets.',
      'Other':         'Set aside 10–15% of your budget as an emergency fund for unexpected expenses.'
    };
    return tips[category] || '';
  }

}
