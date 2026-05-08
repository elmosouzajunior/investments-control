import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

declare global {
  interface Window {
    omegaInvestConfig?: {
      apiUrl?: string;
    };
  }
}

async function loadRuntimeConfig() {
  try {
    const response = await fetch('/app-config.json', { cache: 'no-store' });
    if (!response.ok) return;

    window.omegaInvestConfig = await response.json();
  } catch {
    window.omegaInvestConfig = undefined;
  }
}

loadRuntimeConfig()
  .then(() => bootstrapApplication(App, appConfig))
  .catch((err) => console.error(err));
