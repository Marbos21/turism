import { environmentLocal } from './environment.local';

export const environment = {
  production: true,
  apiUrl: 'https://api.familytrip.app/api/v1',
  mapboxToken: environmentLocal.mapboxToken,
  appName: 'FamilyTrip Assistant'
};
