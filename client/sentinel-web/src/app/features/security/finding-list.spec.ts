import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SecurityFinding } from '../../core/models/api-models';
import { FindingList } from './finding-list';
import { NOW, aFinding } from './test-data';

describe('FindingList', () => {
  let fixture: ComponentFixture<FindingList>;
  let element: HTMLElement;

  function render(findings: SecurityFinding[], pendingKey: string | null = null): void {
    fixture.componentRef.setInput('findings', findings);
    fixture.componentRef.setInput('timeZone', 'Africa/Lagos');
    fixture.componentRef.setInput('now', NOW);
    fixture.componentRef.setInput('pendingKey', pendingKey);
    fixture.detectChanges();
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(FindingList);
    element = fixture.nativeElement as HTMLElement;
  });

  it('shows an empty state', () => {
    render([]);

    expect(element.textContent).toContain('Nothing to review.');
  });

  it('shows each finding with its time in the user zone', () => {
    render([aFinding({ occurredAtUtc: '2026-10-25T09:00:00Z' })]);

    const card = element.querySelector('article')!;
    expect(card.querySelector('h3')?.textContent).toBe('Sign-in from a new country');
    expect(card.querySelector('time')?.textContent).toContain('25 Oct 2026, 10:00'); // Lagos is UTC+1
    expect(card.querySelector('time')?.getAttribute('datetime')).toBe('2026-10-25T09:00:00Z');
    expect(card.textContent).toContain('3 hours ago');
  });

  it('emits the key of the finding the user acknowledges', () => {
    render([aFinding({ key: 'first' }), aFinding({ key: 'second' })]);
    const acknowledged: string[] = [];
    fixture.componentInstance.acknowledge.subscribe((key) => acknowledged.push(key));

    element.querySelectorAll<HTMLButtonElement>('article button')[1].click();

    expect(acknowledged).toEqual(['second']);
  });

  it('disables every button while one acknowledgement is in flight', () => {
    render([aFinding({ key: 'first' }), aFinding({ key: 'second' })], 'first');

    const buttons = [...element.querySelectorAll<HTMLButtonElement>('article button')];
    expect(buttons.every((b) => b.disabled)).toBe(true);
    expect(buttons[0].textContent?.trim()).toBe('Saving…');
    expect(buttons[1].textContent?.trim()).toBe('This was me');
  });
});
