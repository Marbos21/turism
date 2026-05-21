import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiService } from './api.service';
import {
  Family, CreateFamilyRequest, AddFamilyMemberRequest, FamilyMember,
  BudgetLevel, TravelStyle, AgeGroup
} from '../models/family.model';

interface BackendFamily {
  id: string;
  name: string;
  userId: string;
  budgetLevel: number;
  preferredClimate: number;
  travelStyle: number;
  createdAt: string;
  members: BackendFamilyMember[];
}

interface BackendFamilyMember {
  id: string;
  familyId: string;
  name: string;
  birthDate: string;
  age: number;
  interests: string | null;
  specialNeeds: string | null;
}

@Injectable({ providedIn: 'root' })
export class FamilyService {
  constructor(private api: ApiService) {}

  getMyFamilies(): Observable<Family[]> {
    return this.api.get<BackendFamily[]>('/families').pipe(
      map(list => list.map(f => this.toFeFamily(f)))
    );
  }

  getFamilyById(id: number | string): Observable<Family> {
    return this.api.get<BackendFamily>(`/families/${id}`).pipe(
      map(f => this.toFeFamily(f))
    );
  }

  createFamily(request: CreateFamilyRequest): Observable<Family> {
    return this.api.post<BackendFamily>('/families', request).pipe(
      map(f => this.toFeFamily(f))
    );
  }

  updateFamily(id: number | string, request: Partial<CreateFamilyRequest>): Observable<Family> {
    return this.api.put<BackendFamily>(`/families/${id}`, request).pipe(
      map(f => this.toFeFamily(f))
    );
  }

  deleteFamily(id: number | string): Observable<void> {
    return this.api.delete<void>(`/families/${id}`);
  }

  addMember(familyId: number | string, request: AddFamilyMemberRequest): Observable<FamilyMember> {
    return this.api
      .post<BackendFamilyMember>(`/families/${familyId}/members`, this.toBeMemberRequest(request))
      .pipe(map(m => this.toFeMember(m)));
  }

  updateMember(familyId: number | string, memberId: number | string, request: Partial<AddFamilyMemberRequest>): Observable<FamilyMember> {
    return this.api
      .put<BackendFamilyMember>(
        `/families/${familyId}/members/${memberId}`,
        this.toBeMemberRequest(request as AddFamilyMemberRequest)
      )
      .pipe(map(m => this.toFeMember(m)));
  }

  removeMember(familyId: number | string, memberId: number | string): Observable<void> {
    return this.api.delete<void>(`/families/${familyId}/members/${memberId}`);
  }

  private toFeFamily(be: BackendFamily): Family {
    return {
      id: be.id as unknown as number,
      userId: be.userId as unknown as number,
      familyName: be.name ?? '',
      budgetLevel: this.budgetIntToEnum(be.budgetLevel),
      travelStyle: this.travelStyleIntToEnum(be.travelStyle),
      preferredClimates: be.preferredClimate ? [this.climateIntToString(be.preferredClimate)] : [],
      members: (be.members ?? []).map(m => this.toFeMember(m)),
      createdAt: be.createdAt
    };
  }

  private toFeMember(be: BackendFamilyMember): FamilyMember {
    const interests = be.interests
      ? be.interests.split(',').map(s => s.trim()).filter(Boolean)
      : [];
    return {
      id: be.id as unknown as number,
      familyId: be.familyId as unknown as number,
      fullName: be.name ?? '',
      birthDate: be.birthDate,
      ageGroup: this.deriveAgeGroup(be.birthDate),
      interests,
      dietaryRestrictions: be.specialNeeds ?? undefined,
      mobilityNeeds: undefined
    };
  }

  private toBeMemberRequest(fe: AddFamilyMemberRequest): {
    name: string;
    birthDate: string;
    interests: string | null;
    specialNeeds: string | null;
  } {
    const specialNeedsParts = [fe.dietaryRestrictions, fe.mobilityNeeds]
      .filter((v): v is string => !!v && v.trim().length > 0);
    return {
      name: fe.fullName,
      birthDate: fe.birthDate,
      interests: fe.interests?.length ? fe.interests.join(', ') : null,
      specialNeeds: specialNeedsParts.length ? specialNeedsParts.join(' | ') : null
    };
  }

  private budgetIntToEnum(value: number): BudgetLevel {
    switch (value) {
      case 1: return BudgetLevel.Budget;
      case 2: return BudgetLevel.Moderate;
      case 3: return BudgetLevel.Comfort;
      case 4: return BudgetLevel.Luxury;
      default: return BudgetLevel.Moderate;
    }
  }

  private travelStyleIntToEnum(value: number): TravelStyle {
    switch (value) {
      case 1: return TravelStyle.Adventure;
      case 2: return TravelStyle.Relaxation;
      case 3: return TravelStyle.Cultural;
      case 4: return TravelStyle.City;
      default: return TravelStyle.Cultural;
    }
  }

  private climateIntToString(value: number): string {
    switch (value) {
      case 1: return 'Tropical';
      case 2: return 'Mediterranean';
      case 3: return 'Temperate';
      case 4: return 'Mountain';
      default: return 'Temperate';
    }
  }

  private deriveAgeGroup(birthDate: string): AgeGroup {
    const birth = new Date(birthDate);
    if (Number.isNaN(birth.getTime())) return AgeGroup.Adult;
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    if (age < 2) return AgeGroup.Infant;
    if (age < 13) return AgeGroup.Child;
    if (age < 20) return AgeGroup.Teen;
    if (age < 65) return AgeGroup.Adult;
    return AgeGroup.Senior;
  }
}
