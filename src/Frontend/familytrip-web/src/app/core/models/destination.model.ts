export interface Destination {
  id: number;
  name: string;
  country: string;
  region: string;
  description: string;
  imageUrl: string;
  latitude: number;
  longitude: number;
  childFriendlyScore: number;      // 0–10
  averageCostPerDayUsd: number;
  climate: ClimateType;
  bestMonths: number[];
  languages: string[];
  currency: string;
  safetyRating: number;            // 1–5
  tags: string[];
  highlights: string[];
  weatherSummary: WeatherSummary;
  createdAt: string;
}

export enum ClimateType {
  Tropical     = 'Tropical',
  Subtropical  = 'Subtropical',
  Mediterranean= 'Mediterranean',
  Temperate    = 'Temperate',
  Continental  = 'Continental',
  Arid         = 'Arid',
  Polar        = 'Polar',
  Mountain     = 'Mountain'
}

export interface WeatherSummary {
  avgTempCelsius: number;
  rainyDaysPerMonth: number;
  sunHoursPerDay: number;
  humidity: number;
}

export interface DestinationFilter {
  search?: string;
  climate?: ClimateType;
  minChildScore?: number;
  maxCostPerDay?: number;
  country?: string;
  tags?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}
