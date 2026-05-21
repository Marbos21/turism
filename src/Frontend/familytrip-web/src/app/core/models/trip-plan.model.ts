export interface TripPlan {
  id: number;
  userId: number;
  familyId: number;
  familyName: string;
  destinationId: number;
  destinationName: string;
  destinationCountry: string;
  destinationImageUrl: string;
  title: string;
  startDate: string;
  endDate: string;
  durationDays: number;
  status: TripStatus;
  totalBudgetUsd: number;
  actualSpendUsd?: number;
  notes?: string;
  itinerary?: ItineraryDay[];
  budgetBreakdown?: BudgetBreakdown;
  createdAt: string;
  updatedAt?: string;
}

export enum TripStatus {
  Draft     = 'Draft',
  Planned   = 'Planned',
  Active    = 'Active',
  Completed = 'Completed',
  Cancelled = 'Cancelled'
}

export interface ItineraryDay {
  id: number;
  tripPlanId: number;
  dayNumber: number;
  date: string;
  title: string;
  activities: Activity[];
  totalCostUsd: number;
  notes?: string;
}

export interface Activity {
  id: number;
  itineraryDayId: number;
  name: string;
  description: string;
  startTime: string;
  endTime: string;
  type: ActivityType;
  costPerPersonUsd: number;
  location: string;
  latitude?: number;
  longitude?: number;
  isMandatory: boolean;
  isKidFriendly: boolean;
}

export enum ActivityType {
  Accommodation = 'Accommodation',
  Transport     = 'Transport',
  Meal          = 'Meal',
  Attraction    = 'Attraction',
  Shopping      = 'Shopping',
  Recreation    = 'Recreation',
  Free          = 'Free'
}

export interface BudgetBreakdown {
  accommodationUsd: number;
  transportUsd: number;
  foodUsd: number;
  activitiesUsd: number;
  otherUsd: number;
  totalUsd: number;
  numberOfPeople: number;
  perPersonUsd: number;
}

export interface CreateTripPlanRequest {
  familyId: number | string;
  destinationId: number | string;
  title: string;
  startDate: string;
  endDate: string;
  estimatedBudget: number;
  notes?: string;
}
