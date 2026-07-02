import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.clear();
    document.body.classList.remove('dark');
    TestBed.configureTestingModule({});
  });

  it('arranca en tema claro por defecto', () => {
    const service = TestBed.inject(ThemeService);
    expect(service.theme()).toBe('light');
    expect(document.body.classList.contains('dark')).toBeFalse();
  });

  it('toggle cambia a oscuro y lo persiste', () => {
    const service = TestBed.inject(ThemeService);
    service.toggle();
    expect(service.theme()).toBe('dark');
    expect(document.body.classList.contains('dark')).toBeTrue();
    expect(localStorage.getItem('connecthub_theme')).toBe('dark');
  });

  it('un segundo toggle vuelve a claro', () => {
    const service = TestBed.inject(ThemeService);
    service.toggle();
    service.toggle();
    expect(service.theme()).toBe('light');
    expect(document.body.classList.contains('dark')).toBeFalse();
  });
});
