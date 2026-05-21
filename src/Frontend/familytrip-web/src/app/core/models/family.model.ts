export interface FamilyMember {
  id: number;
  familyId: number;
  fullName: string;
  birthDate: string;
  ageGroup: AgeGroup;
  interests: string[];
  dietaryRestrictions?: string;
  mobilityNeeds?: string;
}

export enum AgeGroup {
  Infant  = 'Infant',
  Child   = 'Child',
  Teen    = 'Teen',
  Adult   = 'Adult',
  Senior  = 'Senior'
}

export interface Family {
  id: number;
  userId: number;
  familyName: string;
  budgetLevel: BudgetLevel;
  travelStyle: TravelStyle;
  preferredClimates: string[];
  members: FamilyMember[];
  createdAt: string;
  updatedAt?: string;
}

export enum BudgetLevel {
  Budget   = 'Budget',
  Moderate = 'Moderate',
  Comfort  = 'Comfort',
  Luxury   = 'Luxury'
}

export enum TravelStyle {
  Adventure   = 'Adventure',
  Cultural    = 'Cultural',
  Beach       = 'Beach',
  City        = 'City',
  Nature      = 'Nature',
  Educational = 'Educational',
  Relaxation  = 'Relaxation'
}

export interface CreateFamilyRequest {
  familyName: string;
  budgetLevel: BudgetLevel;
  travelStyle: TravelStyle;
  preferredClimates: string[];
}

export interface AddFamilyMemberRequest {
  fullName: string;
  birthDate: string;
  ageGroup: AgeGroup;
  interests: string[];
  dietaryRestrictions?: string;
  mobilityNeeds?: string;
}
