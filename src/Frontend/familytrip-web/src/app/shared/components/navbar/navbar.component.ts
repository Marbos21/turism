import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.scss']
})
export class NavbarComponent {
  readonly authService = inject(AuthService);
  readonly router      = inject(Router);

  mobileMenuOpen = false;

  navLinks = [
    { label: 'Dashboard',      path: '/dashboard',      icon: 'dashboard' },
    { label: 'My Family',      path: '/family',         icon: 'family_restroom' },
    { label: 'Destinations',   path: '/destinations',   icon: 'explore' },
    { label: 'Trip Planner',   path: '/trip-planner',   icon: 'map' },
    { label: 'Recommendations',path: '/recommendations',icon: 'star' }
  ];

  toggleMobileMenu(): void {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  logout(): void {
    this.authService.logout();
  }

  get userInitials(): string {
    const user = this.authService.currentUser();
    if (!user) return '?';
    return user.fullName
      .split(' ')
      .map(n => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }
}
