import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NavigationItem } from '../../../shared/models/navigation.model';
import { PageKey } from '../../../shared/models/page-key.model';
import { Session } from '../../../shared/models/session.model';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.scss'
})
export class SidebarComponent {
  @Input({ required: true }) session!: Session;
  @Input({ required: true }) activePage!: PageKey;
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
}
