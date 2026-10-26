# Appendix C · The Same Code in Older Angular

If the test environment runs an Angular older than 17, translate as follows.
Everything in this book works in Angular 22 with either column.

| Modern (this book) | Classic (Angular 2–16) |
|---|---|
| `readonly risk = input.required<RiskLevel>();` | `@Input({ required: true }) risk!: RiskLevel;` (`required` from v16; earlier, just `@Input() risk!: RiskLevel;`) |
| `readonly count = input(0);` | `@Input() count = 0;` |
| `readonly review = output<void>();` | `@Output() review = new EventEmitter<void>();` |
| `this.review.emit()` | `this.review.emit()` (same) |
| reading an input: `this.risk()` | reading an input: `this.risk` |
| `computed(() => presentRisk(this.risk()))` | a getter: `get presentation() { return presentRisk(this.risk); }` or `ngOnChanges` |
| `signal(x)`, `.set()`, `()` | a field, or a `BehaviorSubject` with the `async` pipe |
| `@if (x) { ... } @else { ... }` | `<ng-container *ngIf="x; else other">...</ng-container><ng-template #other>...</ng-template>` |
| `@for (f of findings; track f.key) { } @empty { }` | `<div *ngFor="let f of findings; trackBy: trackByKey">` and a separate `*ngIf="!findings.length"` |
| `@switch (kind) { @case ('a') { } }` | `<ng-container [ngSwitch]="kind"><div *ngSwitchCase="'a'">` |
| `inject(SecurityApi)` | `constructor(private readonly api: SecurityApi) {}` |
| standalone `imports: [FindingList]` | declare components in an `NgModule`'s `declarations` |
| `viewChild<ElementRef>('ref')` | `@ViewChild('ref') ref?: ElementRef;` |
| `takeUntilDestroyed()` | a `destroy$ = new Subject<void>()`, `takeUntil(this.destroy$)`, and `ngOnDestroy` |
| functional guard `CanActivateFn` | a class implementing `CanActivate` (functional guards from v14.2) |
| `withInterceptors([fn])` | a class implementing `HttpInterceptor`, provided with `HTTP_INTERCEPTORS` |
| `withComponentInputBinding()` | `ActivatedRoute.queryParamMap.subscribe(...)` |
| Vitest `vi.fn()` | Jasmine `jasmine.createSpy()` |

The container from Chapter 17, in classic form, is a useful exercise: write
it once and the translation becomes automatic.
