export interface Session {
  token: string;
  fullName: string;
  email: string;
  role: 'Master' | 'CompanyUser';
  companyId: string | null;
  companyName: string | null;
}
