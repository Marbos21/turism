import { Component, Input, OnChanges, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss']
})
export class MapComponent implements OnChanges {
  @Input() latitude     = 48.8566;
  @Input() longitude    = 2.3522;
  @Input() zoom         = 10;
  @Input() height       = '350px';
  @Input() locationName = '';

  private sanitizer = inject(DomSanitizer);
  safeMapUrl: SafeResourceUrl = '';

  ngOnChanges(): void {
    const raw = `https://www.openstreetmap.org/export/embed.html?bbox=${this.longitude - 0.1}%2C${this.latitude - 0.1}%2C${this.longitude + 0.1}%2C${this.latitude + 0.1}&layer=mapnik&marker=${this.latitude}%2C${this.longitude}`;
    this.safeMapUrl = this.sanitizer.bypassSecurityTrustResourceUrl(raw);
  }
}
