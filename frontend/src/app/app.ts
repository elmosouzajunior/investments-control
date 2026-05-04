import { CurrencyPipe, DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { ChangeDetectorRef, Component, computed, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { AppShellComponent } from './components/layouts/app-shell/app-shell';
import { LoginPageComponent } from './pages/login/login-page';
import { NavigationItem } from './shared/models/navigation.model';
import { PageKey } from './shared/models/page-key.model';
import { Session } from './shared/models/session.model';

type MessageType = 'success' | 'error';

interface Company {
  id: string;
  name: string;
  cnpj: string;
  tradeName?: string;
  isActive: boolean;
}

interface CompanyUser {
  id: string;
  companyId: string;
  companyName: string;
  fullName: string;
  email: string;
  isActive: boolean;
}

interface Plan {
  id: string;
  name: string;
  description?: string;
  targetAmount: number;
  targetTermYears?: number;
  isActive: boolean;
}

interface SimpleItem {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

interface Institution {
  id: string;
  name: string;
  code?: string;
  notes?: string;
  isActive: boolean;
}

interface ResponsibleUser {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
}

interface Investment {
  id: string;
  planId: string;
  planName: string;
  typeId: string;
  typeName: string;
  institutionId: string;
  institutionName: string;
  responsibleUserId?: string;
  responsibleUserName?: string;
  name: string;
  ticker?: string;
  initialAmount: number;
  startDate: string;
  notes?: string;
  isActive: boolean;
}

interface Operation {
  id: string;
  investmentId: string;
  type: string;
  operationDate: string;
  amount: number;
  description?: string;
}

interface ContributionOperation extends Operation {
  investmentName: string;
}

interface Measurement {
  id: string;
  investmentId: string;
  month: string;
  marketValue: number;
  incomeAmount: number;
  notes?: string;
}

interface IncomeMeasurement extends Measurement {
  investmentName: string;
}

interface DashboardPlan {
  planId: string;
  planName: string;
  currentRealBalance: number;
  currentExpectedBalance: number;
  realProjectedBalance: number;
  expectedProjectedBalance: number;
  realProjectionBalance: number;
  expectedProjectionBalance: number;
  realYieldPercent: number;
  expectedYieldPercent: number;
}

@Component({
  selector: 'app-root',
  imports: [AppShellComponent, CurrencyPipe, DecimalPipe, LoginPageComponent, NgTemplateOutlet, ReactiveFormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly apiUrl = 'http://localhost:5241/api';

  session: Session | null = this.readSession();
  activePage: PageKey = this.session?.role === 'Master' ? 'companies' : 'dashboard';
  loading = false;
  message = '';
  messageType: MessageType = 'success';

  companies: Company[] = [];
  users: CompanyUser[] = [];
  plans: Plan[] = [];
  investmentTypes: SimpleItem[] = [];
  institutions: Institution[] = [];
  responsibleUsers: ResponsibleUser[] = [];
  investments: Investment[] = [];
  operations: Operation[] = [];
  measurements: Measurement[] = [];
  dashboardPlans: DashboardPlan[] = [];
  selectedDashboardPlanId = '';
  dashboardProjectionYears = 30;
  selectedInvestment: Investment | null = null;
  initialAmountDisplay = this.formatCurrency(0);
  dashboardOpen = true;
  cadastrosOpen = true;
  lancamentosOpen = true;
  selectedContributionPlanId = '';
  contributionOperations: ContributionOperation[] = [];
  contributionModalOpen = false;
  editingContributionId: string | null = null;
  editingContributionInvestmentId: string | null = null;
  contributionAmountDisplay = this.formatCurrency(0);
  selectedWithdrawalPlanId = '';
  withdrawalOperations: ContributionOperation[] = [];
  withdrawalModalOpen = false;
  editingWithdrawalId: string | null = null;
  editingWithdrawalInvestmentId: string | null = null;
  withdrawalAmountDisplay = this.formatCurrency(0);
  selectedIncomePlanId = '';
  incomeMeasurements: IncomeMeasurement[] = [];
  incomeModalOpen = false;
  editingIncomeId: string | null = null;
  editingIncomeInvestmentId: string | null = null;
  incomeAmountDisplay = this.formatCurrency(0);

  editingCompanyId: string | null = null;
  editingUserId: string | null = null;
  editingPlanId: string | null = null;
  editingTypeId: string | null = null;
  editingInstitutionId: string | null = null;
  editingInvestmentId: string | null = null;
  companyModalOpen = false;
  userModalOpen = false;
  planModalOpen = false;
  typeModalOpen = false;
  institutionModalOpen = false;
  investmentModalOpen = false;

  loginForm = this.fb.nonNullable.group({
    email: ['master@omegainvest.com', [Validators.required, Validators.email]],
    password: ['Master@123', Validators.required]
  });

  companyForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    tradeName: [''],
    cnpj: ['', Validators.required],
    isActive: [true]
  });

  userForm = this.fb.nonNullable.group({
    companyId: ['', Validators.required],
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    password: [''],
    isActive: [true]
  });

  planForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    targetAmount: [0, [Validators.min(0), Validators.max(100)]],
    targetTermYears: [0, [Validators.min(0), Validators.max(30)]],
    isActive: [true]
  });

  typeForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    description: [''],
    isActive: [true]
  });

  institutionForm = this.fb.nonNullable.group({
    name: ['', Validators.required],
    isActive: [true]
  });

  investmentForm = this.fb.nonNullable.group({
    planId: ['', Validators.required],
    typeId: ['', Validators.required],
    institutionId: ['', Validators.required],
    responsibleUserId: ['', Validators.required],
    name: ['', Validators.required],
    ticker: [''],
    initialAmount: [0, Validators.min(0)],
    startDate: [new Date().toISOString().slice(0, 10), Validators.required],
    notes: [''],
    isActive: [true]
  });

  operationForm = this.fb.nonNullable.group({
    type: ['Contribution', Validators.required],
    operationDate: [new Date().toISOString().slice(0, 10), Validators.required],
    amount: [0, Validators.min(0.01)],
    description: ['']
  });

  contributionForm = this.fb.nonNullable.group({
    investmentId: ['', Validators.required],
    operationDate: [new Date().toISOString().slice(0, 10), Validators.required],
    amount: [0, Validators.min(0.01)]
  });

  withdrawalForm = this.fb.nonNullable.group({
    investmentId: ['', Validators.required],
    operationDate: [new Date().toISOString().slice(0, 10), Validators.required],
    amount: [0, Validators.min(0.01)]
  });

  incomeForm = this.fb.nonNullable.group({
    investmentId: ['', Validators.required],
    month: [new Date().toISOString().slice(0, 10), Validators.required],
    marketValue: [0, Validators.min(0)]
  });

  measurementForm = this.fb.nonNullable.group({
    month: [new Date().toISOString().slice(0, 10), Validators.required],
    marketValue: [0, Validators.min(0)],
    incomeAmount: [0],
    notes: ['']
  });

  constructor() {
    if (this.session) {
      void this.loadCurrentPage();
    }
  }

  get menu(): NavigationItem[] {
    return this.session?.role === 'Master'
      ? [
          { key: 'companies', label: 'Empresas', icon: 'bi-buildings' },
          { key: 'users', label: 'Usuários', icon: 'bi-people' }
        ]
      : [
          { key: 'plans', label: 'Planos', icon: 'bi-bullseye' },
          { key: 'types', label: 'Categorias', icon: 'bi-grid-3x3-gap' },
          { key: 'institutions', label: 'Instituições', icon: 'bi-bank' },
          { key: 'investments', label: 'Investimentos', icon: 'bi-pie-chart' }
        ];
  }

  get cadastroMenu(): NavigationItem[] {
    return this.session?.role === 'Master'
      ? []
      : this.menu
          .filter((item) => ['plans', 'types', 'institutions', 'investments'].includes(item.key))
          .sort((a, b) => a.label.localeCompare(b.label));
  }

  get lancamentoMenu(): NavigationItem[] {
    return this.session?.role === 'Master'
      ? []
      : ([
          { key: 'income', label: 'Rendimento', icon: 'bi-cash-coin' },
          { key: 'withdrawals', label: 'Retirada', icon: 'bi-box-arrow-down-left' },
          { key: 'contributions', label: 'Aporte', icon: 'bi-box-arrow-in-up-right' }
        ] satisfies NavigationItem[])
          .sort((a, b) => a.label.localeCompare(b.label));
  }

  get dashboardMenu(): NavigationItem[] {
    return this.session?.role === 'Master'
      ? []
      : [{ key: 'dashboard', label: 'Dashboard', icon: 'bi-speedometer2' }];
  }

  get pageTitle() {
    return [...this.dashboardMenu, ...this.menu, ...this.lancamentoMenu].find((item) => item.key === this.activePage)?.label ?? 'Omega Invest';
  }

  get activeCompanies() {
    return this.companies.filter((x) => x.isActive);
  }

  get activePlans() {
    return this.plans.filter((x) => x.isActive);
  }

  get activeTypes() {
    return this.investmentTypes.filter((x) => x.isActive);
  }

  get activeInstitutions() {
    return this.institutions.filter((x) => x.isActive);
  }

  get activeInvestments() {
    return this.investments.filter((x) => x.isActive);
  }

  get investmentsForContributionPlan() {
    return this.investments.filter((x) => x.planId === this.selectedContributionPlanId && x.isActive);
  }

  get investmentsForWithdrawalPlan() {
    return this.investments.filter((x) => x.planId === this.selectedWithdrawalPlanId && x.isActive);
  }

  get investmentsForIncomePlan() {
    return this.investments.filter((x) => x.planId === this.selectedIncomePlanId && x.isActive);
  }

  get selectedDashboardPlan() {
    return this.dashboardPlans.find((plan) => plan.planId === this.selectedDashboardPlanId) ?? null;
  }

  get selectedDashboardGap() {
    const plan = this.selectedDashboardPlan;
    return plan ? plan.currentRealBalance - plan.currentExpectedBalance : 0;
  }

  get selectedDashboardProjectionGap() {
    const plan = this.selectedDashboardPlan;
    return plan ? plan.realProjectionBalance - plan.expectedProjectionBalance : 0;
  }

  get selectedContributionInvestmentResponsible() {
    const investmentId = this.contributionForm.controls.investmentId.value;
    const investment = this.investments.find((x) => x.id === investmentId);
    return investment?.responsibleUserName ?? 'Não definido';
  }

  get selectedWithdrawalInvestmentResponsible() {
    const investmentId = this.withdrawalForm.controls.investmentId.value;
    const investment = this.investments.find((x) => x.id === investmentId);
    return investment?.responsibleUserName ?? 'Não definido';
  }

  get selectedIncomeInvestmentResponsible() {
    const investmentId = this.incomeForm.controls.investmentId.value;
    const investment = this.investments.find((x) => x.id === investmentId);
    return investment?.responsibleUserName ?? 'Não definido';
  }

  async login() {
    if (this.loginForm.invalid) return;
    this.loading = true;
    this.message = '';
    let session: Session;
    try {
      session = await firstValueFrom(this.http.post<Session>(`${this.apiUrl}/auth/login`, this.loginForm.getRawValue()));
    } catch {
      this.showMessage('Não foi possível entrar. Confira usuário e senha.', 'error');
      this.loading = false;
      return;
    }

    this.session = session;
    localStorage.setItem('omega-invest-session', JSON.stringify(session));
    this.activePage = session.role === 'Master' ? 'companies' : 'dashboard';
    this.loading = false;
    this.cdr.detectChanges();

    try {
      await this.loadCurrentPage();
      this.cdr.detectChanges();
    } catch {
      this.showMessage('Login realizado, mas não foi possível carregar os dados iniciais.', 'error');
      this.cdr.detectChanges();
    }
  }

  logout() {
    localStorage.removeItem('omega-invest-session');
    this.session = null;
    this.message = '';
  }

  async setPage(page: PageKey) {
    this.activePage = page;
    await this.loadCurrentPage();
    this.cdr.detectChanges();
  }

  toggleCadastros() {
    this.cadastrosOpen = !this.cadastrosOpen;
  }

  toggleLancamentos() {
    this.lancamentosOpen = !this.lancamentosOpen;
  }

  toggleDashboard() {
    this.dashboardOpen = !this.dashboardOpen;
  }

  async loadCurrentPage() {
    if (this.session?.role === 'Master') {
      await this.loadMaster();
      return;
    }

    await this.loadTenantBaseData();
    if (this.activePage === 'dashboard') {
      await this.loadDashboard();
    }
    if (this.activePage === 'investments') {
      await this.loadInvestments();
    }
    if (this.activePage === 'contributions') {
      await this.loadContributionPage();
    }
    if (this.activePage === 'withdrawals') {
      await this.loadWithdrawalPage();
    }
    if (this.activePage === 'income') {
      await this.loadIncomePage();
    }
  }

  async loadMaster() {
    if (this.activePage === 'users') {
      const [companies, users] = await Promise.all([
        this.get<Company[]>('/companies'),
        this.get<CompanyUser[]>('/users')
      ]);
      this.companies = companies;
      this.users = users;
      return;
    }

    this.companies = await this.get<Company[]>('/companies');
  }

  async loadTenantBaseData() {
    if (this.activePage === 'dashboard' || this.activePage === 'plans' || this.activePage === 'investments' || this.activePage === 'contributions' || this.activePage === 'withdrawals' || this.activePage === 'income') {
      this.plans = await this.get<Plan[]>('/plans');
      this.clearInvalidLaunchSelections();
    }
    if (this.activePage === 'types' || this.activePage === 'investments') this.investmentTypes = await this.get<SimpleItem[]>('/investment-types');
    if (this.activePage === 'institutions' || this.activePage === 'investments') this.institutions = await this.get<Institution[]>('/institutions');
    if (this.activePage === 'investments') this.responsibleUsers = await this.get<ResponsibleUser[]>('/investments/responsible-users');
  }

  async saveCompany() {
    await this.save('/companies', this.editingCompanyId, this.companyForm.getRawValue());
    this.resetCompany();
    await this.loadMaster();
  }

  openCompanyModal() {
    this.editingCompanyId = null;
    this.companyForm.reset({ name: '', tradeName: '', cnpj: '', isActive: true });
    this.companyModalOpen = true;
  }

  editCompany(company: Company) {
    this.editingCompanyId = company.id;
    this.companyForm.patchValue(company);
    this.companyModalOpen = true;
  }

  resetCompany() {
    this.editingCompanyId = null;
    this.companyModalOpen = false;
    this.companyForm.reset({ name: '', tradeName: '', cnpj: '', isActive: true });
  }

  async saveUser() {
    await this.save('/users', this.editingUserId, this.userForm.getRawValue());
    this.resetUser();
    await this.loadMaster();
  }

  openUserModal() {
    this.editingUserId = null;
    this.userForm.reset({ companyId: '', fullName: '', email: '', password: '', isActive: true });
    this.userModalOpen = true;
  }

  editUser(user: CompanyUser) {
    this.editingUserId = user.id;
    this.userForm.patchValue({ ...user, password: '' });
    this.userModalOpen = true;
  }

  resetUser() {
    this.editingUserId = null;
    this.userModalOpen = false;
    this.userForm.reset({ companyId: '', fullName: '', email: '', password: '', isActive: true });
  }

  async savePlan() {
    const body = this.planForm.getRawValue();
    if (this.editingPlanId) {
      await this.save('/plans', this.editingPlanId, body);
      this.plans = await this.get<Plan[]>('/plans');
    } else {
      const created = await this.create<Plan>('/plans', body);
      this.plans = [...this.plans, created].sort((a, b) => a.name.localeCompare(b.name));
      this.showMessage('Registro cadastrado.');
    }
    this.resetPlan();
    this.cdr.detectChanges();
  }

  openPlanModal() {
    this.editingPlanId = null;
    this.planForm.reset({ name: '', targetAmount: 0, targetTermYears: 0, isActive: true });
    this.planModalOpen = true;
  }

  editPlan(plan: Plan) {
    this.editingPlanId = plan.id;
    this.planForm.patchValue({ ...plan, targetTermYears: plan.targetTermYears ?? 0 });
    this.planModalOpen = true;
  }

  resetPlan() {
    this.editingPlanId = null;
    this.planModalOpen = false;
    this.planForm.reset({ name: '', targetAmount: 0, targetTermYears: 0, isActive: true });
  }

  async saveType() {
    const body = this.typeForm.getRawValue();
    if (this.editingTypeId) {
      await this.save('/investment-types', this.editingTypeId, body);
      this.investmentTypes = await this.get<SimpleItem[]>('/investment-types');
    } else {
      const created = await this.create<SimpleItem>('/investment-types', body);
      this.investmentTypes = [...this.investmentTypes, created].sort((a, b) => a.name.localeCompare(b.name));
      this.showMessage('Registro cadastrado.');
    }
    this.resetType();
    this.cdr.detectChanges();
  }

  openTypeModal() {
    this.editingTypeId = null;
    this.typeForm.reset({ name: '', description: '', isActive: true });
    this.typeModalOpen = true;
  }

  editType(type: SimpleItem) {
    this.editingTypeId = type.id;
    this.typeForm.patchValue(type);
    this.typeModalOpen = true;
  }

  resetType() {
    this.editingTypeId = null;
    this.typeModalOpen = false;
    this.typeForm.reset({ name: '', description: '', isActive: true });
  }

  async saveInstitution() {
    const body = this.institutionForm.getRawValue();
    try {
      if (this.editingInstitutionId) {
        await this.save('/institutions', this.editingInstitutionId, body);
        this.institutions = await this.get<Institution[]>('/institutions');
      } else {
        const created = await this.create<Institution>('/institutions', body);
        this.institutions = [...this.institutions, created].sort((a, b) => a.name.localeCompare(b.name));
        this.showMessage('Registro cadastrado.');
      }
      this.resetInstitution();
      this.cdr.detectChanges();
    } catch (error) {
      this.showMessage(this.errorMessage(error, 'Não foi possível salvar a instituição financeira.'), 'error');
    }
  }

  openInstitutionModal() {
    this.editingInstitutionId = null;
    this.institutionForm.reset({ name: '', isActive: true });
    this.institutionModalOpen = true;
  }

  editInstitution(institution: Institution) {
    this.editingInstitutionId = institution.id;
    this.institutionForm.patchValue(institution);
    this.institutionModalOpen = true;
  }

  resetInstitution() {
    this.editingInstitutionId = null;
    this.institutionModalOpen = false;
    this.institutionForm.reset({ name: '', isActive: true });
  }

  async loadInvestments() {
    this.investments = await this.get<Investment[]>('/investments');
  }

  async loadDashboard() {
    await this.loadInvestments();
    const today = new Date();
    const projectionYears = this.normalizedDashboardProjectionYears();
    const projectionDate = this.addYears(today, projectionYears);
    const rows: DashboardPlan[] = [];

    for (const plan of this.activePlans) {
      const planInvestments = this.investments.filter((investment) => investment.planId === plan.id);
      let currentRealBalance = 0;
      let currentExpectedBalance = 0;
      let realProjectedBalance = 0;
      let expectedProjectedBalance = 0;
      let realProjectionBalance = 0;
      let expectedProjectionBalance = 0;
      let investedBase = 0;

      for (const investment of planInvestments) {
        const [operations, measurements] = await Promise.all([
          this.get<Operation[]>(`/investments/${investment.id}/operations`),
          this.get<Measurement[]>(`/investments/${investment.id}/measurements`)
        ]);
        const latestMeasurement = [...measurements].sort((a, b) => b.month.localeCompare(a.month))[0];
        const netInvested = this.netInvestedAmount(investment, operations);
        const realBalance = latestMeasurement?.marketValue ?? netInvested;
        const expectedBalance = this.projectCashflows(investment, operations, plan.targetAmount, today);
        const planEndDate = this.addYears(new Date(investment.startDate), plan.targetTermYears ?? 0);
        const expectedFinalBalance = this.projectCashflows(investment, operations, plan.targetAmount, planEndDate);
        const remainingYears = Math.max(this.yearsBetween(today, planEndDate), 0);
        const expectedAnnualRate = plan.targetAmount / 100;

        investedBase += netInvested;
        currentRealBalance += realBalance;
        currentExpectedBalance += expectedBalance;
        expectedProjectedBalance += expectedFinalBalance;
        realProjectedBalance += realBalance * Math.pow(1 + expectedAnnualRate, remainingYears);
        realProjectionBalance += realBalance * Math.pow(1 + expectedAnnualRate, projectionYears);
        expectedProjectionBalance += this.projectCashflows(investment, operations, plan.targetAmount, projectionDate);
      }

      rows.push({
        planId: plan.id,
        planName: plan.name,
        currentRealBalance,
        currentExpectedBalance,
        realProjectedBalance,
        expectedProjectedBalance,
        realProjectionBalance,
        expectedProjectionBalance,
        realYieldPercent: investedBase > 0 ? ((currentRealBalance / investedBase) - 1) * 100 : 0,
        expectedYieldPercent: investedBase > 0 ? ((currentExpectedBalance / investedBase) - 1) * 100 : 0
      });
    }

    this.dashboardPlans = rows;
    this.ensureDashboardPlanSelection();
  }

  setDashboardPlan(planId: string) {
    this.selectedDashboardPlanId = this.dashboardPlans.some((plan) => plan.planId === planId) ? planId : '';
  }

  async setDashboardProjectionYears(value: string) {
    const years = Number(value);
    this.dashboardProjectionYears = Number.isFinite(years) ? Math.min(Math.max(years, 1), 50) : 30;
    await this.loadDashboard();
    this.cdr.detectChanges();
  }

  async loadContributionPage() {
    await this.loadInvestments();
    await this.loadContributionOperations();
  }

  async loadWithdrawalPage() {
    await this.loadInvestments();
    await this.loadWithdrawalOperations();
  }

  async loadIncomePage() {
    await this.loadInvestments();
    await this.loadIncomeMeasurements();
  }

  async setContributionPlan(planId: string) {
    this.selectedContributionPlanId = this.isActivePlan(planId) ? planId : '';
    await this.loadContributionOperations();
    this.cdr.detectChanges();
  }

  async setWithdrawalPlan(planId: string) {
    this.selectedWithdrawalPlanId = this.isActivePlan(planId) ? planId : '';
    await this.loadWithdrawalOperations();
    this.cdr.detectChanges();
  }

  async setIncomePlan(planId: string) {
    this.selectedIncomePlanId = this.isActivePlan(planId) ? planId : '';
    await this.loadIncomeMeasurements();
    this.cdr.detectChanges();
  }

  async loadContributionOperations() {
    if (!this.selectedContributionPlanId) {
      this.contributionOperations = [];
      return;
    }

    const investments = this.investmentsForContributionPlan;
    const operationLists = await Promise.all(
      investments.map(async (investment) => {
        const operations = await this.get<Operation[]>(`/investments/${investment.id}/operations`);
        return operations
          .filter((operation) => operation.type === 'Contribution')
          .map((operation) => ({ ...operation, investmentName: investment.name }));
      })
    );

    this.contributionOperations = operationLists
      .flat()
      .sort((a, b) => this.compareIsoDateDesc(a.operationDate, b.operationDate));
  }

  async loadWithdrawalOperations() {
    if (!this.selectedWithdrawalPlanId) {
      this.withdrawalOperations = [];
      return;
    }

    const investments = this.investmentsForWithdrawalPlan;
    const operationLists = await Promise.all(
      investments.map(async (investment) => {
        const operations = await this.get<Operation[]>(`/investments/${investment.id}/operations`);
        return operations
          .filter((operation) => operation.type === 'Withdrawal')
          .map((operation) => ({ ...operation, investmentName: investment.name }));
      })
    );

    this.withdrawalOperations = operationLists
      .flat()
      .sort((a, b) => this.compareIsoDateDesc(a.operationDate, b.operationDate));
  }

  async loadIncomeMeasurements() {
    if (!this.selectedIncomePlanId) {
      this.incomeMeasurements = [];
      return;
    }

    const investments = this.investmentsForIncomePlan;
    const measurementLists = await Promise.all(
      investments.map(async (investment) => {
        const measurements = await this.get<Measurement[]>(`/investments/${investment.id}/measurements`);
        return measurements.map((measurement) => ({ ...measurement, investmentName: investment.name }));
      })
    );

    this.incomeMeasurements = measurementLists
      .flat()
      .sort((a, b) => this.compareIsoDateDesc(a.month, b.month));
  }

  async saveInvestment() {
    await this.save('/investments', this.editingInvestmentId, this.investmentForm.getRawValue());
    this.resetInvestment();
    await this.loadInvestments();
  }

  openInvestmentModal() {
    this.resetInvestment();
    this.investmentModalOpen = true;
  }

  editInvestment(investment: Investment) {
    this.editingInvestmentId = investment.id;
    this.investmentForm.patchValue(investment);
    this.initialAmountDisplay = this.formatCurrency(investment.initialAmount);
    this.investmentModalOpen = true;
  }

  resetInvestment() {
    this.editingInvestmentId = null;
    this.investmentModalOpen = false;
    this.initialAmountDisplay = this.formatCurrency(0);
    this.investmentForm.reset({
      planId: this.activePlans[0]?.id ?? '',
      typeId: this.activeTypes[0]?.id ?? '',
      institutionId: this.activeInstitutions[0]?.id ?? '',
      responsibleUserId: this.responsibleUsers[0]?.id ?? '',
      name: '',
      ticker: '',
      initialAmount: 0,
      startDate: new Date().toISOString().slice(0, 10),
      notes: '',
      isActive: true
    });
  }

  updateInitialAmount(value: string) {
    const amount = this.parseCurrency(value);
    this.investmentForm.patchValue({ initialAmount: amount });
    this.initialAmountDisplay = value;
  }

  formatInitialAmount() {
    this.initialAmountDisplay = this.formatCurrency(this.investmentForm.controls.initialAmount.value);
  }

  formatDisplayDate(value?: string) {
    if (!value) return '';
    const [year, month, day] = value.slice(0, 10).split('-');
    return day && month && year ? `${day}/${month}/${year}` : value;
  }

  dashboardBarWidth(value: number, max: number) {
    if (max <= 0) return 0;
    return Math.max(4, Math.min(100, (value / max) * 100));
  }

  openContributionModal(operation?: ContributionOperation) {
    this.editingContributionId = operation?.id ?? null;
    this.editingContributionInvestmentId = operation?.investmentId ?? null;
    const investmentId = operation?.investmentId ?? this.investmentsForContributionPlan[0]?.id ?? '';
    const amount = operation?.amount ?? 0;
    this.contributionAmountDisplay = this.formatCurrency(amount);
    this.contributionForm.reset({
      investmentId,
      operationDate: operation?.operationDate ?? new Date().toISOString().slice(0, 10),
      amount
    });
    this.contributionModalOpen = true;
  }

  closeContributionModal() {
    this.contributionModalOpen = false;
    this.editingContributionId = null;
    this.editingContributionInvestmentId = null;
    this.contributionAmountDisplay = this.formatCurrency(0);
  }

  updateContributionAmount(value: string) {
    const amount = this.parseCurrency(value);
    this.contributionForm.patchValue({ amount });
    this.contributionAmountDisplay = value;
  }

  formatContributionAmount() {
    this.contributionAmountDisplay = this.formatCurrency(this.contributionForm.controls.amount.value);
  }

  async saveContribution() {
    if (this.contributionForm.invalid) return;

    const formValue = this.contributionForm.getRawValue();
    const body = {
      type: 'Contribution',
      operationDate: formValue.operationDate,
      amount: formValue.amount,
      description: null
    };

    if (this.editingContributionId && this.editingContributionInvestmentId) {
      await firstValueFrom(this.http.put(
        `${this.apiUrl}/investments/${this.editingContributionInvestmentId}/operations/${this.editingContributionId}`,
        body,
        { headers: this.headers() }));
      this.showMessage('Aporte atualizado.');
    } else {
      await this.create<Operation>(`/investments/${formValue.investmentId}/operations`, body);
      this.showMessage('Aporte cadastrado.');
    }

    this.closeContributionModal();
    await this.loadContributionOperations();
  }

  async deleteContribution(operation: ContributionOperation) {
    await firstValueFrom(this.http.delete(
      `${this.apiUrl}/investments/${operation.investmentId}/operations/${operation.id}`,
      { headers: this.headers() }));
    this.showMessage('Aporte excluído.');
    await this.loadContributionOperations();
  }

  openWithdrawalModal(operation?: ContributionOperation) {
    this.editingWithdrawalId = operation?.id ?? null;
    this.editingWithdrawalInvestmentId = operation?.investmentId ?? null;
    const investmentId = operation?.investmentId ?? this.investmentsForWithdrawalPlan[0]?.id ?? '';
    const amount = operation?.amount ?? 0;
    this.withdrawalAmountDisplay = this.formatCurrency(amount);
    this.withdrawalForm.reset({
      investmentId,
      operationDate: operation?.operationDate ?? new Date().toISOString().slice(0, 10),
      amount
    });
    this.withdrawalModalOpen = true;
  }

  closeWithdrawalModal() {
    this.withdrawalModalOpen = false;
    this.editingWithdrawalId = null;
    this.editingWithdrawalInvestmentId = null;
    this.withdrawalAmountDisplay = this.formatCurrency(0);
  }

  updateWithdrawalAmount(value: string) {
    const amount = this.parseCurrency(value);
    this.withdrawalForm.patchValue({ amount });
    this.withdrawalAmountDisplay = value;
  }

  formatWithdrawalAmount() {
    this.withdrawalAmountDisplay = this.formatCurrency(this.withdrawalForm.controls.amount.value);
  }

  async saveWithdrawal() {
    if (this.withdrawalForm.invalid) return;

    const formValue = this.withdrawalForm.getRawValue();
    const body = {
      type: 'Withdrawal',
      operationDate: formValue.operationDate,
      amount: formValue.amount,
      description: null
    };

    if (this.editingWithdrawalId && this.editingWithdrawalInvestmentId) {
      await firstValueFrom(this.http.put(
        `${this.apiUrl}/investments/${this.editingWithdrawalInvestmentId}/operations/${this.editingWithdrawalId}`,
        body,
        { headers: this.headers() }));
      this.showMessage('Retirada atualizada.');
    } else {
      await this.create<Operation>(`/investments/${formValue.investmentId}/operations`, body);
      this.showMessage('Retirada cadastrada.');
    }

    this.closeWithdrawalModal();
    await this.loadWithdrawalOperations();
  }

  async deleteWithdrawal(operation: ContributionOperation) {
    await firstValueFrom(this.http.delete(
      `${this.apiUrl}/investments/${operation.investmentId}/operations/${operation.id}`,
      { headers: this.headers() }));
    this.showMessage('Retirada excluída.');
    await this.loadWithdrawalOperations();
  }

  openIncomeModal(measurement?: IncomeMeasurement) {
    this.editingIncomeId = measurement?.id ?? null;
    this.editingIncomeInvestmentId = measurement?.investmentId ?? null;
    const investmentId = measurement?.investmentId ?? this.investmentsForIncomePlan[0]?.id ?? '';
    const amount = measurement?.marketValue ?? 0;
    this.incomeAmountDisplay = this.formatCurrency(amount);
    this.incomeForm.reset({
      investmentId,
      month: measurement?.month ?? new Date().toISOString().slice(0, 10),
      marketValue: amount
    });
    this.incomeModalOpen = true;
  }

  closeIncomeModal() {
    this.incomeModalOpen = false;
    this.editingIncomeId = null;
    this.editingIncomeInvestmentId = null;
    this.incomeAmountDisplay = this.formatCurrency(0);
  }

  updateIncomeAmount(value: string) {
    const amount = this.parseCurrency(value);
    this.incomeForm.patchValue({ marketValue: amount });
    this.incomeAmountDisplay = value;
  }

  formatIncomeAmount() {
    this.incomeAmountDisplay = this.formatCurrency(this.incomeForm.controls.marketValue.value);
  }

  async saveIncome() {
    if (this.incomeForm.invalid) return;

    const formValue = this.incomeForm.getRawValue();
    const body = {
      month: formValue.month,
      marketValue: formValue.marketValue,
      incomeAmount: 0,
      notes: null
    };

    if (this.editingIncomeId && this.editingIncomeInvestmentId) {
      await firstValueFrom(this.http.put(
        `${this.apiUrl}/investments/${this.editingIncomeInvestmentId}/measurements/${this.editingIncomeId}`,
        body,
        { headers: this.headers() }));
      this.showMessage('Rendimento atualizado.');
    } else {
      await this.create<Measurement>(`/investments/${formValue.investmentId}/measurements`, body);
      this.showMessage('Rendimento cadastrado.');
    }

    this.closeIncomeModal();
    await this.loadIncomeMeasurements();
  }

  async deleteIncome(measurement: IncomeMeasurement) {
    await firstValueFrom(this.http.delete(
      `${this.apiUrl}/investments/${measurement.investmentId}/measurements/${measurement.id}`,
      { headers: this.headers() }));
    this.showMessage('Rendimento excluído.');
    await this.loadIncomeMeasurements();
  }

  async selectInvestment(investment: Investment) {
    this.selectedInvestment = investment;
    this.operations = await this.get<Operation[]>(`/investments/${investment.id}/operations`);
    this.measurements = await this.get<Measurement[]>(`/investments/${investment.id}/measurements`);
  }

  async saveOperation() {
    if (!this.selectedInvestment) return;
    await this.post(`/investments/${this.selectedInvestment.id}/operations`, this.operationForm.getRawValue());
    await this.selectInvestment(this.selectedInvestment);
  }

  async saveMeasurement() {
    if (!this.selectedInvestment) return;
    await this.post(`/investments/${this.selectedInvestment.id}/measurements`, this.measurementForm.getRawValue());
    await this.selectInvestment(this.selectedInvestment);
  }

  private async save(path: string, id: string | null, body: unknown) {
    if (id) {
      await firstValueFrom(this.http.put(`${this.apiUrl}${path}/${id}`, body, { headers: this.headers() }));
      this.showMessage('Registro atualizado.');
    } else {
      await this.post(path, body);
      this.showMessage('Registro cadastrado.');
    }
  }

  private async post(path: string, body: unknown) {
    await firstValueFrom(this.http.post(`${this.apiUrl}${path}`, body, { headers: this.headers() }));
  }

  private async create<T>(path: string, body: unknown): Promise<T> {
    return firstValueFrom(this.http.post<T>(`${this.apiUrl}${path}`, body, { headers: this.headers() }));
  }

  private async get<T>(path: string): Promise<T> {
    const separator = path.includes('?') ? '&' : '?';
    return firstValueFrom(this.http.get<T>(`${this.apiUrl}${path}${separator}_=${Date.now()}`, { headers: this.headers() }));
  }

  private clearInvalidLaunchSelections() {
    if (!this.isActivePlan(this.selectedContributionPlanId)) {
      this.selectedContributionPlanId = '';
      this.contributionOperations = [];
    }
    if (!this.isActivePlan(this.selectedWithdrawalPlanId)) {
      this.selectedWithdrawalPlanId = '';
      this.withdrawalOperations = [];
    }
    if (!this.isActivePlan(this.selectedIncomePlanId)) {
      this.selectedIncomePlanId = '';
      this.incomeMeasurements = [];
    }
  }

  private isActivePlan(planId: string) {
    return !!planId && this.plans.some((plan) => plan.id === planId && plan.isActive);
  }

  private compareIsoDateDesc(left: string, right: string) {
    return right.localeCompare(left);
  }

  private ensureDashboardPlanSelection() {
    if (this.dashboardPlans.some((plan) => plan.planId === this.selectedDashboardPlanId)) return;

    const retirementPlan = this.dashboardPlans.find((plan) => plan.planName.toLocaleLowerCase('pt-BR') === 'aposentadoria');
    this.selectedDashboardPlanId = retirementPlan?.planId ?? this.dashboardPlans[0]?.planId ?? '';
  }

  private normalizedDashboardProjectionYears() {
    return Math.min(Math.max(this.dashboardProjectionYears || 30, 1), 50);
  }

  private headers() {
    return new HttpHeaders({
      Authorization: `Bearer ${this.session?.token ?? ''}`,
      'Cache-Control': 'no-cache',
      Pragma: 'no-cache'
    });
  }

  private showMessage(message: string, type: MessageType = 'success') {
    this.message = message;
    this.messageType = type;
  }

  private errorMessage(error: unknown, fallback: string) {
    if (error instanceof HttpErrorResponse && typeof error.error?.message === 'string') {
      return error.error.message;
    }

    return fallback;
  }

  private parseCurrency(value: string) {
    const normalized = value
      .replace(/[^\d,.-]/g, '')
      .replace(/\./g, '')
      .replace(',', '.');

    const amount = Number(normalized);
    return Number.isFinite(amount) ? amount : 0;
  }

  private formatCurrency(value: number) {
    return new Intl.NumberFormat('pt-BR', {
      style: 'currency',
      currency: 'BRL'
    }).format(value);
  }

  private netInvestedAmount(investment: Investment, operations: Operation[]) {
    return operations.reduce((total, operation) => {
      if (operation.type === 'Contribution') return total + operation.amount;
      if (operation.type === 'Withdrawal') return total - operation.amount;
      return total;
    }, investment.initialAmount);
  }

  private projectCashflows(investment: Investment, operations: Operation[], annualPercent: number, targetDate: Date) {
    const annualRate = annualPercent / 100;
    const cashflows = [
      { date: new Date(investment.startDate), amount: investment.initialAmount },
      ...operations
        .filter((operation) => operation.type === 'Contribution' || operation.type === 'Withdrawal')
        .map((operation) => ({
          date: new Date(operation.operationDate),
          amount: operation.type === 'Contribution' ? operation.amount : -operation.amount
        }))
    ];

    return cashflows.reduce((total, cashflow) => {
      const years = Math.max(this.yearsBetween(cashflow.date, targetDate), 0);
      return total + cashflow.amount * Math.pow(1 + annualRate, years);
    }, 0);
  }

  private yearsBetween(start: Date, end: Date) {
    return (end.getTime() - start.getTime()) / (365.25 * 24 * 60 * 60 * 1000);
  }

  private addYears(date: Date, years: number) {
    const result = new Date(date);
    result.setDate(result.getDate() + Math.round(years * 365.25));
    return result;
  }

  private readSession(): Session | null {
    const raw = localStorage.getItem('omega-invest-session');
    return raw ? JSON.parse(raw) : null;
  }
}
