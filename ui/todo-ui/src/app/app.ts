import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TodoService } from './todo.service';
import { Todo } from './todo.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private todoService = inject(TodoService);

  todos = signal<Todo[]>([]);
  newTitle = '';

  ngOnInit() {
    this.load();
  }

  load() {
    this.todoService.getAll().subscribe((t) => this.todos.set(t));
  }

  add() {
    const title = this.newTitle.trim();
    if (!title) return;
    this.todoService.create(title).subscribe(() => {
      this.newTitle = '';
      this.load();
    });
  }

  toggle(id: number) {
    this.todoService.toggle(id).subscribe(() => this.load());
  }

  delete(id: number) {
    this.todoService.delete(id).subscribe(() => this.load());
  }
}
