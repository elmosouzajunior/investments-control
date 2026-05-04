import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss'
})
export class LoginPageComponent {
  @Input({ required: true }) form!: FormGroup;
  @Input() loading = false;
  @Input() message = '';

  @Output() submitLogin = new EventEmitter<void>();
}
