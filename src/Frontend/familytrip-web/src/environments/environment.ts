import { environmentLocal } from './environment.local';

export const environment = {
  production: false,
  apiUrl: '/api/v1',
  mapboxToken: environmentLocal.mapboxToken,
  appName: 'FamilyTrip Assistant'
};
