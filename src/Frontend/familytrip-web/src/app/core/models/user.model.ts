export interface User {
  id: string;
  email: string;
  fullName: string;
  role?: UserRole;
  createdAt?: string;
  updatedAt?: string;
}

export enum UserRole {
  Admin  = 'Admin',
  Parent = 'Parent',
  User   = 'User'
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  userInfo: User;
  expiresAt: string;
}
