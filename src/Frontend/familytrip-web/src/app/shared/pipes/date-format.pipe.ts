import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'dateFormat', standalone: true })
export class DateFormatPipe implements PipeTransform {
  transform(value: string | Date | null | undefined, format: 'short' | 'medium' | 'long' | 'relative' = 'medium'): string {
    if (!value) return '';

    const date = typeof value === 'string' ? new Date(value) : value;
    if (isNaN(date.getTime())) return '';

    switch (format) {
      case 'short':
        return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });

      case 'medium':
        return date.toLocaleDateString('en-US', { weekday: 'short', month: 'long', day: 'numeric', year: 'numeric' });

      case 'long':
        return date.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });

      case 'relative': {
        const now   = new Date();
        const diffMs = now.getTime() - date.getTime();
        const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

        if (diffDays === 0)  return 'Today';
        if (diffDays === 1)  return 'Yesterday';
        if (diffDays === -1) return 'Tomorrow';
        if (diffDays < 0)   return `In ${Math.abs(diffDays)} days`;
        if (diffDays < 7)   return `${diffDays} days ago`;
        if (diffDays < 30)  return `${Math.floor(diffDays / 7)} weeks ago`;
        if (diffDays < 365) return `${Math.floor(diffDays / 30)} months ago`;
        return `${Math.floor(diffDays / 365)} years ago`;
      }

      default:
        return date.toLocaleDateString();
    }
  }
}
