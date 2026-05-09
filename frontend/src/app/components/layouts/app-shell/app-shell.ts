import { Component, EventEmitter, Input, Output } from '@angular/core';
import { SidebarComponent } from '../sidebar/sidebar';
import { NavigationItem } from '../../../shared/models/navigation.model';
import { PageKey } from '../../../shared/models/page-key.model';
import { Session } from '../../../shared/models/session.model';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [SidebarComponent],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss'
})
export class AppShellComponent {
  @Input({ required: true }) session!: Session;
  @Input({ required: true }) activePage!: PageKey;
  @Input({ required: true }) pageTitle!: string;
  @Input() message = '';
  @Input() messageType: 'success' | 'error' = 'success';
  @Input() masterMenu: NavigationItem[] = [];
  @Input() dashboardMenu: NavigationItem[] = [];
  @Input() cadastroMenu: NavigationItem[] = [];
  @Input() lancamentoMenu: NavigationItem[] = [];
  @Input() dashboardOpen = true;
  @Input() cadastrosOpen = true;
  @Input() lancamentosOpen = true;

  @Output() pageChange = new EventEmitter<PageKey>();
  @Output() dashboardToggle = new EventEmitter<void>();
  @Output() cadastrosToggle = new EventEmitter<void>();
  @Output() lancamentosToggle = new EventEmitter<void>();
  @Output() logout = new EventEmitter<void>();

  mobileMenuOpen = false;

  toggleMobileMenu() {
    this.mobileMenuOpen = !this.mobileMenuOpen;
  }

  closeMobileMenu() {
    this.mobileMenuOpen = false;
  }

  selectPage(page: PageKey) {
    this.pageChange.emit(page);
    this.closeMobileMenu();
  }

  requestLogout() {
    this.closeMobileMenu();
    this.logout.emit();
  }
}
