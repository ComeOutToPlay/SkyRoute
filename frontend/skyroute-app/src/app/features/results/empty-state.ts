import { Component } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `<p class="empty" role="status">No flights found for this search.</p>`,
  styles: [
    `
      .empty {
        padding: 1rem;
        border: 1px solid #d9dce3;
        border-radius: 8px;
        background: #f9fafc;
      }
    `,
  ],
})
export class EmptyStateComponent {}
