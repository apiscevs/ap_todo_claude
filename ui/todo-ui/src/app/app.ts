import { Component, computed, effect, inject, OnInit, signal } from '@angular/core';
import { CommonModule, DOCUMENT } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TodoService } from './todo.service';
import { Todo, Priority } from './todo.model';

type Filter = 'all' | 'active' | 'completed';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private todoService = inject(TodoService);
  private document = inject(DOCUMENT);

  readonly PRIORITIES: Priority[] = ['LOW', 'MEDIUM', 'HIGH'];

  todos = signal<Todo[]>([]);
  newTitle = signal('');
  newDescription = signal('');
  newPriority = signal<Priority>('MEDIUM');
  filter = signal<Filter>('all');
  errorMessage = signal('');
  darkMode = signal(false);

  filteredTodos = computed(() => {
    const todos = this.todos();
    switch (this.filter()) {
      case 'active':
        return todos.filter((t) => !t.isCompleted);
      case 'completed':
        return todos.filter((t) => t.isCompleted);
      default:
        return todos;
    }
  });

  constructor() {
    const saved = localStorage.getItem('todo-dark-mode');
    if (saved === 'true' || (!saved && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
      this.darkMode.set(true);
    }

    effect(() => {
      const dark = this.darkMode();
      this.document.documentElement.setAttribute('data-theme', dark ? 'dark' : 'light');
      localStorage.setItem('todo-dark-mode', String(dark));
    });
  }

  ngOnInit() {
    this.load();
  }

  load() {
    this.todoService.getAll().subscribe({
      next: (t) => this.todos.set(t),
      error: () => this.showError('Failed to load todos'),
    });
  }

  add() {
    const title = this.newTitle().trim();
    if (!title) return;
    const description = this.newDescription().trim();
    this.todoService.create(title, this.newPriority(), description).subscribe({
      next: () => {
        this.newTitle.set('');
        this.newDescription.set('');
        this.newPriority.set('MEDIUM');
        this.load();
      },
      error: () => this.showError('Failed to add todo'),
    });
  }

  toggle(id: number) {
    this.todoService.toggle(id).subscribe({
      next: () => this.load(),
      error: () => this.showError('Failed to update todo'),
    });
  }

  deleteTodo(id: number) {
    this.todoService.delete(id).subscribe({
      next: () => this.load(),
      error: () => this.showError('Failed to delete todo'),
    });
  }

  setFilter(f: Filter) {
    this.filter.set(f);
  }

  toggleDarkMode() {
    this.darkMode.update((v) => !v);
  }

  getPriorityLabel(priority: Priority): string {
    return priority.charAt(0) + priority.slice(1).toLowerCase();
  }

  private showError(msg: string) {
    this.errorMessage.set(msg);
    setTimeout(() => this.errorMessage.set(''), 4000);
  }
}
