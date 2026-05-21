import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/dashboard',
    pathMatch: 'full'
  },
  {
    path: 'auth',
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        loadComponent: () =>
          import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () =>
          import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      },
      {
        path: '',
        redirectTo: 'login',
        pathMatch: 'full'
      }
    ]
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'family',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/family/family-profile/family-profile.component').then(m => m.FamilyProfileComponent)
  },
  {
    path: 'destinations',
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/destinations/destination-list/destination-list.component').then(m => m.DestinationListComponent)
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./features/destinations/destination-detail/destination-detail.component').then(m => m.DestinationDetailComponent)
      }
    ]
  },
  {
    path: 'trip-planner',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/trip-planner/trip-planner.component').then(m => m.TripPlannerComponent)
  },
  {
    path: 'itinerary/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/itinerary/itinerary-view.component').then(m => m.ItineraryViewComponent)
  },
  {
    path: 'budget/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/budget/budget-estimator.component').then(m => m.BudgetEstimatorComponent)
  },
  {
    path: 'recommendations',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/recommendations/recommendations.component').then(m => m.RecommendationsComponent)
  },
  {
    path: '**',
    redirectTo: '/dashboard'
  }
];
