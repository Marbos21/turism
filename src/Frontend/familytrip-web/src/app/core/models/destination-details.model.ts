import { Destination } from './destination.model';

export interface WeatherSnapshot {
  averageTempCelsius: number;
  minTempCelsius: number;
  maxTempCelsius: number;
  rainyDays: number;
  summary: string;
}

export interface PointOfInterest {
  name: string;
  category?: string;
  rating?: number;
  userRatingsTotal?: number;
  address?: string;
}

export interface HotelSample {
  name: string;
  rating?: number;
  address?: string;
}

export interface HotelAvailability {
  hotelsFound: number;
  sampleHotels: HotelSample[];
}

export interface DestinationDetails {
  destination: Destination;
  weather: WeatherSnapshot | null;
  attractions: PointOfInterest[];
  hotels: HotelAvailability | null;
}
