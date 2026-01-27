import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Todo } from './todo.model';

@Injectable({ providedIn: 'root' })
export class TodoService {
  private http = inject(HttpClient);
  private url = '/api/todos';

  getAll() {
    return this.http.get<Todo[]>(this.url);
  }

  create(title: string) {
    return this.http.post<Todo>(this.url, { title });
  }

  toggle(id: number) {
    return this.http.put<Todo>(`${this.url}/${id}/toggle`, {});
  }

  delete(id: number) {
    return this.http.delete(`${this.url}/${id}`);
  }
}
