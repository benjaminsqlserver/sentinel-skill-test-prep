import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RiskLevel } from '../../core/models/api-models';
import { SecurityBanner } from './security-banner';
import { NOW } from './test-data';

describe('SecurityBanner', () => {
  let fixture: ComponentFixture<SecurityBanner>;
  let element: HTMLElement;

  // Inputs are set the way a parent template would set them, through the
  // component ref, so the test exercises the real input() declarations.
  function render(riskLevel: RiskLevel, findingCount: number): void {
    fixture.componentRef.setInput('riskLevel', riskLevel);
    fixture.componentRef.setInput('findingCount', findingCount);
    fixture.componentRef.setInput('assessedAtUtc', '2026-10-25T11:55:00Z');
    fixture.componentRef.setInput('now', NOW);
    fixture.detectChanges();
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(SecurityBanner);
    element = fixture.nativeElement as HTMLElement;
  });

  it('says all clear and offers nothing to review when risk is None', () => {
    render('None', 0);

    expect(element.querySelector('h2')?.textContent).toContain('No suspicious activity');
    expect(element.querySelector('button')).toBeNull();
    expect(element.querySelector('section')?.classList).toContain('tone-ok');
  });

  it('tells the user to act when risk is High', () => {
    render('High', 3);

    expect(element.querySelector('h2')?.textContent).toContain('Someone may be using your account');
    expect(element.querySelector('section')?.classList).toContain('tone-danger');
    expect(element.querySelector('section')?.classList).toContain('banner'); // static class survives [class]
  });

  it('pluralises the review button', () => {
    render('Medium', 1);
    expect(element.querySelector('button')?.textContent?.trim()).toBe('Review 1 item');

    render('Medium', 2);
    expect(element.querySelector('button')?.textContent?.trim()).toBe('Review 2 items');
  });

  it('shows when the check ran, relative to now', () => {
    render('None', 0);

    expect(element.querySelector('.assessed')?.textContent).toContain('5 minutes ago');
    expect(element.querySelector('.assessed')?.getAttribute('title')).toBe('2026-10-25 11:55:00 UTC');
  });

  it('emits review when the button is clicked', () => {
    render('High', 2);
    const review = vi.fn();
    fixture.componentInstance.review.subscribe(review);

    element.querySelector<HTMLButtonElement>('button')!.click();

    expect(review).toHaveBeenCalledOnce();
  });
});
