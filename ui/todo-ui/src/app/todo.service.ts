import { Injectable, inject } from '@angular/core';
import { map } from 'rxjs/operators';
import {
  GetTodosGQL,
  CreateTodoGQL,
  UpdateTodoGQL,
  ToggleTodoGQL,
  DeleteTodoGQL,
  CreateTodoInput,
  UpdateTodoInput
} from './generated/graphql';
import { Todo, Priority } from './todo.model';

@Injectable({ providedIn: 'root' })
export class TodoService {
  private getTodosGQL = inject(GetTodosGQL);
  private createTodoGQL = inject(CreateTodoGQL);
  private updateTodoGQL = inject(UpdateTodoGQL);
  private toggleTodoGQL = inject(ToggleTodoGQL);
  private deleteTodoGQL = inject(DeleteTodoGQL);

  getAll() {
    return this.getTodosGQL
      .fetch()
      .pipe(
        map(result => {
          const todos = result.data?.todos || [];
          return todos.map(t => ({
            id: t.id,
            title: t.title,
            description: t.description ?? '',
            isCompleted: t.isCompleted,
            priority: t.priority as Priority,
            startAtUtc: t.startAtUtc ?? null,
            endAtUtc: t.endAtUtc ?? null
          })) as Todo[];
        })
      );
  }

  create(title: string, priority: Priority, description?: string, startAtUtc?: string | null, endAtUtc?: string | null) {
    // TODO: Remove 'as any' after codegen generates proper CreateTodoInput type
    const input: CreateTodoInput = {
      title,
      priority: priority as any,
      description: description ?? '',
      startAtUtc: startAtUtc ?? null,
      endAtUtc: endAtUtc ?? null
    };
    return this.createTodoGQL
      .mutate({
        variables: { input },
        refetchQueries: ['GetTodos'],
        awaitRefetchQueries: true
      })
      .pipe(
        map(result => {
          const todo = result.data?.createTodo;
          if (!todo) throw new Error('Failed to create todo');
          return {
            id: todo.id,
            title: todo.title,
            description: todo.description ?? '',
            isCompleted: todo.isCompleted,
            priority: todo.priority as Priority,
            startAtUtc: todo.startAtUtc ?? null,
            endAtUtc: todo.endAtUtc ?? null
          } as Todo;
        })
      );
  }

  update(id: number, input: UpdateTodoInput) {
    return this.updateTodoGQL
      .mutate({
        variables: { id, input },
        refetchQueries: ['GetTodos'],
        awaitRefetchQueries: true
      })
      .pipe(
        map(result => {
          const todo = result.data?.updateTodo;
          if (!todo) throw new Error('Failed to update todo');
          return {
            id: todo.id,
            title: todo.title,
            description: todo.description ?? '',
            isCompleted: todo.isCompleted,
            priority: todo.priority as Priority,
            startAtUtc: todo.startAtUtc ?? null,
            endAtUtc: todo.endAtUtc ?? null
          } as Todo;
        })
      );
  }

  toggle(id: number) {
    return this.toggleTodoGQL
      .mutate({
        variables: { id },
        // TODO: Remove 'as any' after codegen generates proper TodoPriority type
        optimisticResponse: {
          __typename: 'Mutation',
          toggleTodo: {
            __typename: 'TodoItem',
            id,
            title: '',
            description: '',
            isCompleted: true,
            priority: 'MEDIUM' as any,
            startAtUtc: null,
            endAtUtc: null
          }
        },
        refetchQueries: ['GetTodos']
      })
      .pipe(
        map(result => {
          const todo = result.data?.toggleTodo;
          if (!todo) throw new Error('Failed to toggle todo');
          return {
            id: todo.id,
            title: todo.title,
            description: todo.description ?? '',
            isCompleted: todo.isCompleted,
            priority: todo.priority as Priority,
            startAtUtc: todo.startAtUtc ?? null,
            endAtUtc: todo.endAtUtc ?? null
          } as Todo;
        })
      );
  }

  delete(id: number) {
    return this.deleteTodoGQL
      .mutate({
        variables: { id },
        update(cache) {
          cache.evict({ id: `TodoItem:${id}` });
          cache.gc();
        }
      })
      .pipe(
        map(result => {
          if (result.data?.deleteTodo !== true) {
            throw new Error('Failed to delete todo');
          }
          return result.data.deleteTodo;
        })
      );
  }
}
