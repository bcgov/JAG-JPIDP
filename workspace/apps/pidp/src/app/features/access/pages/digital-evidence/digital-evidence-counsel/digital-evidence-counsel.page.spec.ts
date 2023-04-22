import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DigitalEvidenceCounselPage } from './digital-evidence-counsel.page';

describe('DigitalEvidenceCounselPage', () => {
  let component: DigitalEvidenceCounselPage;
  let fixture: ComponentFixture<DigitalEvidenceCounselPage>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [DigitalEvidenceCounselPage],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DigitalEvidenceCounselPage);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
