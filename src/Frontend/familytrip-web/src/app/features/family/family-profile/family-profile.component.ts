import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { FamilyService } from '../../../core/services/family.service';
import {
  Family, FamilyMember, BudgetLevel, TravelStyle, AgeGroup,
  CreateFamilyRequest, AddFamilyMemberRequest
} from '../../../core/models/family.model';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-family-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, DateFormatPipe],
  templateUrl: './family-profile.component.html',
  styleUrls: ['./family-profile.component.scss']
})
export class FamilyProfileComponent implements OnInit {
  private fb            = inject(FormBuilder);
  private familyService = inject(FamilyService);

  family         = signal<Family | null>(null);
  isLoading      = signal(true);
  isSaving       = signal(false);
  showFamilyForm = signal(false);
  showMemberForm = signal(false);
  editingMember  = signal<FamilyMember | null>(null);
  errorMessage   = signal('');
  successMessage = signal('');

  readonly budgetLevels  = Object.values(BudgetLevel);
  readonly travelStyles  = Object.values(TravelStyle);
  readonly ageGroups     = Object.values(AgeGroup);
  readonly climateOptions= ['Tropical', 'Mediterranean', 'Temperate', 'Arid', 'Mountain', 'Subtropical'];
  readonly interestOptions = [
    'Beach', 'Hiking', 'Museums', 'Food & Cuisine', 'Water Sports',
    'Theme Parks', 'Wildlife', 'History', 'Shopping', 'Adventure Sports',
    'Photography', 'Local Markets', 'Art', 'Music', 'Cycling'
  ];

  familyForm!: FormGroup;
  memberForm!: FormGroup;

  ngOnInit(): void {
    this.initForms();
    this.loadFamily();
  }

  private initForms(): void {
    this.familyForm = this.fb.group({
      familyName:        ['', [Validators.required, Validators.minLength(2)]],
      budgetLevel:       [BudgetLevel.Moderate, Validators.required],
      travelStyle:       [TravelStyle.Cultural, Validators.required],
      preferredClimates: [[]]
    });

    this.memberForm = this.fb.group({
      fullName:             ['', [Validators.required, Validators.minLength(2)]],
      birthDate:            ['', Validators.required],
      ageGroup:             [AgeGroup.Adult, Validators.required],
      interests:            [[]],
      dietaryRestrictions:  [''],
      mobilityNeeds:        ['']
    });
  }

  private loadFamily(): void {
    this.familyService.getMyFamilies().subscribe({
      next: (families) => {
        if (families.length > 0) {
          this.family.set(families[0]);
          this.patchFamilyForm(families[0]);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  private patchFamilyForm(family: Family): void {
    this.familyForm.patchValue({
      familyName:        family.familyName,
      budgetLevel:       family.budgetLevel,
      travelStyle:       family.travelStyle,
      preferredClimates: family.preferredClimates
    });
  }

  // Backend uses numeric enums:
  // BudgetLevel: Budget(1), Moderate(2), Premium(3), Luxury(4)
  // TravelStyle: Adventure(1), Relaxation(2), Cultural(3), Mixed(4)
  // Climate: Tropical(1), Mediterranean(2), Continental(3), Arctic(4)
  private toBackendBudgetLevel(level: BudgetLevel): number {
    switch (level) {
      case BudgetLevel.Budget:
        return 1;
      case BudgetLevel.Moderate:
        return 2;
      case (BudgetLevel as any).Premium: // in case it exists in older UI code
      case (BudgetLevel as any).Comfort:
        return 3;
      case BudgetLevel.Luxury:
        return 4;
      default:
        // Safe fallback: moderate
        return 2;
    }
  }

  private toBackendTravelStyle(style: TravelStyle): number {
    switch (style) {
      case TravelStyle.Adventure:
        return 1;
      case TravelStyle.Relaxation:
        return 2;
      case TravelStyle.Cultural:
        return 3;
      default:
        // Beach/City/Nature/Educational -> Mixed on backend
        return 4;
    }
  }

  private toBackendClimate(climate: string | undefined): number {
    switch (climate) {
      case 'Tropical':
        return 1;
      case 'Mediterranean':
        return 2;
      case 'Temperate':
      case 'Arid':
        return 3;
      case 'Mountain':
        return 4;
      case 'Subtropical':
        return 1;
      default:
        return 3;
    }
  }

  saveFamily(): void {
    if (this.familyForm.invalid) {
      this.familyForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    // Backend expects { Name, BudgetLevel, PreferredClimate, TravelStyle }
    const formValue = this.familyForm.value as {
      familyName: string;
      budgetLevel: BudgetLevel;
      travelStyle: TravelStyle;
      preferredClimates: string[];
    };

    const request = {
      Name: formValue.familyName,
      BudgetLevel: this.toBackendBudgetLevel(formValue.budgetLevel),
      PreferredClimate: this.toBackendClimate(formValue.preferredClimates?.[0]),
      TravelStyle: this.toBackendTravelStyle(formValue.travelStyle)
    } as any as CreateFamilyRequest;
    const f = this.family();

    const obs$ = f
      ? this.familyService.updateFamily(f.id, request)
      : this.familyService.createFamily(request);

    obs$.subscribe({
      next: (updated) => {
        this.family.set(updated);
        this.isSaving.set(false);
        this.showFamilyForm.set(false);
        this.showSuccess('Family profile saved successfully!');
      },
      error: () => {
        this.isSaving.set(false);
        this.errorMessage.set('Failed to save family profile. Please try again.');
      }
    });
  }

  openAddMember(): void {
    this.editingMember.set(null);
    this.memberForm.reset({ ageGroup: AgeGroup.Adult, interests: [] });
    this.showMemberForm.set(true);
  }

  openEditMember(member: FamilyMember): void {
    this.editingMember.set(member);
    this.memberForm.patchValue({
      fullName:            member.fullName,
      birthDate:           member.birthDate.split('T')[0],
      ageGroup:            member.ageGroup,
      interests:           member.interests,
      dietaryRestrictions: member.dietaryRestrictions || '',
      mobilityNeeds:       member.mobilityNeeds || ''
    });
    this.showMemberForm.set(true);
  }

  saveMember(): void {
    if (this.memberForm.invalid) {
      this.memberForm.markAllAsTouched();
      return;
    }

    const f = this.family();
    if (!f) return;

    this.isSaving.set(true);
    const request: AddFamilyMemberRequest = this.memberForm.value;
    const editing = this.editingMember();

    const obs$ = editing
      ? this.familyService.updateMember(f.id, editing.id, request)
      : this.familyService.addMember(f.id, request);

    obs$.subscribe({
      next: () => {
        this.loadFamily();
        this.isSaving.set(false);
        this.showMemberForm.set(false);
        this.showSuccess(editing ? 'Member updated!' : 'Member added to the family!');
      },
      error: () => {
        this.isSaving.set(false);
        this.errorMessage.set('Failed to save member. Please try again.');
      }
    });
  }

  deleteMember(member: FamilyMember): void {
    const f = this.family();
    if (!f) return;
    if (!confirm(`Remove ${member.fullName} from the family?`)) return;

    this.familyService.removeMember(f.id, member.id).subscribe({
      next: () => {
        this.loadFamily();
        this.showSuccess(`${member.fullName} removed from the family.`);
      }
    });
  }

  toggleInterest(interest: string): void {
    const current: string[] = this.memberForm.get('interests')!.value || [];
    const updated = current.includes(interest)
      ? current.filter(i => i !== interest)
      : [...current, interest];
    this.memberForm.get('interests')!.setValue(updated);
  }

  isInterestSelected(interest: string): boolean {
    return (this.memberForm.get('interests')!.value || []).includes(interest);
  }

  toggleClimate(climate: string): void {
    const current: string[] = this.familyForm.get('preferredClimates')!.value || [];
    const updated = current.includes(climate)
      ? current.filter(c => c !== climate)
      : [...current, climate];
    this.familyForm.get('preferredClimates')!.setValue(updated);
  }

  isClimateSelected(climate: string): boolean {
    return (this.familyForm.get('preferredClimates')!.value || []).includes(climate);
  }

  getMemberAge(birthDate: string): number {
    const birth = new Date(birthDate);
    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const m = today.getMonth() - birth.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
    return age;
  }

  getAgeGroupIcon(group: AgeGroup): string {
    const icons: Record<AgeGroup, string> = {
      [AgeGroup.Infant]: 'child_care',
      [AgeGroup.Child]:  'face',
      [AgeGroup.Teen]:   'person',
      [AgeGroup.Adult]:  'person',
      [AgeGroup.Senior]: 'elderly'
    };
    return icons[group] || 'person';
  }

  private showSuccess(msg: string): void {
    this.successMessage.set(msg);
    this.errorMessage.set('');
    setTimeout(() => this.successMessage.set(''), 4000);
  }
}
